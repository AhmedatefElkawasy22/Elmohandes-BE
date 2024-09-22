using elmohandes.Server.Data;
using elmohandes.Server.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using static Azure.Core.HttpHeader;

namespace elmohandes.Server.Sevises
{
    public class OrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SmtpSettings _smtpSettings;
        public OrderRepository(ApplicationDbContext context, IHttpContextAccessor contextAccessor, IEmailSender emailSender, IOptions<SmtpSettings> smtpSettings, IUnitOfWork unitOfWork)
        {
            _context = context;
            _contextAccessor = contextAccessor;
            _emailSender = emailSender;
            _smtpSettings = smtpSettings.Value;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> ConfirmOrderAsync(string? Notes)
        {
            try
            {
                string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return "User is not authenticated.";

                User? user = await _context.Users.SingleOrDefaultAsync(e => e.Id == userId);
                if (user == null) return "User information could not be retrieved.";

                Cart? cart = await _context.Carts.FirstOrDefaultAsync(e => e.UserId == userId);
                if (cart == null) return "There are no products in the shopping cart yet.";

                List<CartProduct> products = await _context.CartProducts
                    .Where(e => e.CartId == cart.Id)
                    .Include(e => e.Product)
                    .ToListAsync();
                if (products.Count == 0) return "There are no products in the shopping cart yet.";

                double totalPrice = products.Sum(item => item.Product.Price * item.CountProduct);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the order
                    Order order = new Order()
                    {
                        UserId = userId,
                        UserName = user.Name,
                        EmailUser = user.Email,
                        AddressUser = user.Address,
                        PhoneNumberUser = user.PhoneNumber,
                        ToltaPrice = totalPrice,
                        OrderTime = DateTime.Now,
                        DeliveredTime = null,
                        Notes = !string.IsNullOrEmpty(Notes) ? Notes : null
                    };

                    await _context.Orders.AddAsync(order);
                    await _context.SaveChangesAsync();

                    // Set data in OrderItems
                    List<OrderItems> orderItemsList = new List<OrderItems>();
                    foreach (var product in products)
                    {
                        OrderItems orderItem = new OrderItems()
                        {
                            ProductId = product.Product.Id,
                            OrderId = order.Id,
                            CountOfProduct = product.CountProduct
                        };
                        orderItemsList.Add(orderItem);
                    }

                    await _context.OrderItems.AddRangeAsync(orderItemsList);
                    await _context.SaveChangesAsync();

                    // Remove from CartProducts and clear cart
                    _context.CartProducts.RemoveRange(products);
                    _context.Carts.Remove(cart);
                    await _context.SaveChangesAsync();

                    // Update product quantities
                    foreach (var product in products)
                    {
                        int newQuantity = product.Product.Quantity - product.CountProduct;
                        await _unitOfWork.Product.UpdateQuantity(product.Product.Id, newQuantity);
                    }

                    // Commit the transaction
                    await transaction.CommitAsync();

                    // Send confirmation emails asynchronously
                    bool isArabic = user.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await SendOrderConfirmationEmailToUserAsync(order, products, user.Email, isArabic);
                    }

                    await SendOrderConfirmationEmailToAdminAsync(order, products, _smtpSettings.SenderEmail);

                    return "Order has been confirmed successfully , check your email for details";
                }
                catch (Exception ex)
                {
                    // Rollback in case of an error
                    await transaction.RollbackAsync();
                    return $"An error occurred while confirming the order: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> CancelOrderAsync(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // Begin a transaction

            try
            {
                string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return "User is not authenticated or an error occurred.";

                bool isAdmin = _contextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

                // Retrieve the order and its items with product details
                Order? order = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(op => op.product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return "Order not found.";

                // If the user is not an admin, ensure the order belongs to the user
                if (!isAdmin && order.UserId != userId)
                    return "Order does not belong to you.";

                // Update product quantities after canceling the order
                foreach (var item in order.Items)
                {
                    if (item.product != null)
                    {
                        int newQuantity = item.product.Quantity + item.CountOfProduct;
                        await _unitOfWork.Product.UpdateQuantity(item.product.Id, newQuantity);
                    }
                }

                // Remove the order and its associated items
                if (order.Items.Any())
                    _context.OrderItems.RemoveRange(order.Items);

                Order temp = order;
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                // Send email to the user & Admin that the order has been cancelled
                await SendOrderCancelledEmailToAdminAsync(temp, _smtpSettings.SenderEmail);
                User? user = _context.Users.SingleOrDefault(e => e.Id == userId);
                if (user == null) return "User is not authenticated or an error occurred.";

                bool isArabic = user.Name.Any(c => c >= 0x0600 && c <= 0x06FF);
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await SendOrderCancelledEmailToUserAsync(temp, user.Email, isArabic);
                }

                await transaction.CommitAsync(); // Commit the transaction if everything is successful

                return "Order has been canceled successfully , check your email for details";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Rollback the transaction if any error occurs
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> DeliveredOrderAsync(int orderId)
        {
            string? userId = _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return "User is not authenticated or an error occurred.";

            bool isAdmin = _contextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

            Order? order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return "Order not found.";

            // If the user is not an admin, ensure the order belongs to the user
            if (!isAdmin && order.UserId != userId)
                return "Order does not belong to you.";

            // Mark the order as delivered by setting DeliveredTime
            order.DeliveredTime = DateTime.Now;

            // Save changes to the database
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return "Order has been marked as delivered successfully.";
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(int orderId)
        {

            Order? order = await _context.Orders.AsNoTracking().SingleOrDefaultAsync(e => e.Id == orderId);
            if (order == null) return null;


            OrderDTO orderDTO = new OrderDTO()
            {
                Id = orderId,
                UserId = order.UserId,
                UserName = order.UserName,
                DeliveredTime = order.DeliveredTime,
                AddressUser = order.AddressUser,
                EmailUser = order.EmailUser,
                Notes = order.Notes,
                OrderTime = order.OrderTime,
                PhoneNumberUser = order.PhoneNumberUser,
                ToltaPrice = order.ToltaPrice
            };


            var productsOrder = new List<Dictionary<string, int>>();


            var temp = await _context.OrderItems
                .Where(e => e.OrderId == orderId)
                .Select(e => new
                {
                    e.CountOfProduct,
                    ProductName = _context.Products
                        .Where(p => p.Id == e.ProductId)
                        .Select(p => p.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Add the product name and quantity to the productsOrder list
            foreach (var item in temp)
            {
                productsOrder.Add(new Dictionary<string, int> (){{ item.ProductName ?? "Unknown Product", item.CountOfProduct }});
            }
          
            orderDTO.Products = productsOrder;

            return orderDTO;
        }

        private async Task SendOrderConfirmationEmailToUserAsync(Order order, List<CartProduct> products, string toEmail, bool isArabic)
        {
            try
            {
                string subject = isArabic ? "تأكيد الطلب - شركة المهندس" : "Order Confirmation - El-MOHANDES COMPANY";
                string body = isArabic ? $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333;
            background-color: #f4f4f4;
            padding: 20px;
        }}
        .email-container {{
            background-color: #ffffff;
            border-radius: 10px;
            max-width: 600px;
            margin: auto;
            padding: 30px;
            box-shadow: 0 0 15px rgba(0, 0, 0, 0.15);
        }}
        h2 {{
            color: #2C3E50;
            font-size: 26px;
            font-weight: 700;
            margin-bottom: 20px;
            border-bottom: 2px solid #4CAF50;
            padding-bottom: 10px;
        }}
        p {{
            color: #555;
            font-size: 16px;
            line-height: 1.8;
            margin-bottom: 15px;
        }}
        .order-details {{
            background-color: #f1f1f1;
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 20px;
            border-left: 5px solid #4CAF50;
        }}
        .product-list {{
            margin-top: 15px;
            padding-left: 20px;
        }}
        .product-list li {{
            margin-bottom: 10px;
            font-size: 15px;
        }}
        .total-price {{
            font-weight: 700;
            color: #E74C3C;
            margin-top: 15px;
            font-size: 18px;
        }}
        .footer {{
            text-align: center;
            font-size: 14px;
            color: #888;
            margin-top: 30px;
        }}
        .cta-button {{
            background-color: #4CAF50;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            display: inline-block;
            margin-top: 20px;
        }}
        .cta-button:hover {{
            background-color: #45A049;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <h2>مرحبا , {order.UserName}</h2>
        <p>شكرا لطلبك ❤️. تم استلام طلبك بنجاح. إليك تفاصيل طلبك:</p>
        <div class='order-details'>
            <p><strong>رقم الطلب:</strong> {order.Id}</p>
            <p><strong>العنوان:</strong> {order.AddressUser}</p>
            <p><strong>الهاتف:</strong> {order.PhoneNumberUser}</p>
            <p><strong>وقت الطلب:</strong> {order.OrderTime}</p>
            <p><strong> الحد الاقصي للوصول :</strong> {DateTime.Now.AddDays(5)}</p>
            <p><strong>ملاحظات:</strong> {(string.IsNullOrEmpty(order.Notes) ? "لا توجد ملاحظات" : order.Notes)}</p>
        </div>
        <h3>المنتجات:</h3>
        <ul class='product-list'>
            {string.Join("", products.Select(item => $"<li>{item.Product.Name}: {item.Product.Price} x {item.CountProduct} = {item.Product.Price * item.CountProduct} جنيه مصري</li>"))}
        </ul>
        <p class='total-price'>إجمالي السعر: {order.ToltaPrice} جنيه مصري</p>
        <p class='footer'>شكراً لثقتك بنا😊<br/>المهندس عبد الحميد القوصي، الرئيس التنفيذي لشركة المهندس</p>
    </div>
</body>
</html>" : $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333;
            background-color: #f4f4f4;
            padding: 20px;
        }}
        .email-container {{
            background-color: #ffffff;
            border-radius: 10px;
            max-width: 600px;
            margin: auto;
            padding: 30px;
            box-shadow: 0 0 15px rgba(0, 0, 0, 0.15);
        }}
        h2 {{
            color: #2C3E50;
            font-size: 26px;
            font-weight: 700;
            margin-bottom: 20px;
            border-bottom: 2px solid #4CAF50;
            padding-bottom: 10px;
        }}
        p {{
            color: #555;
            font-size: 16px;
            line-height: 1.8;
            margin-bottom: 15px;
        }}
        .order-details {{
            background-color: #f1f1f1;
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 20px;
            border-left: 5px solid #4CAF50;
        }}
        .product-list {{
            margin-top: 15px;
            padding-left: 20px;
        }}
        .product-list li {{
            margin-bottom: 10px;
            font-size: 15px;
        }}
        .total-price {{
            font-weight: 700;
            color: #E74C3C;
            margin-top: 15px;
            font-size: 18px;
        }}
        .footer {{
            text-align: center;
            font-size: 14px;
            color: #888;
            margin-top: 30px;
        }}
        .cta-button {{
            background-color: #4CAF50;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            display: inline-block;
            margin-top: 20px;
        }}
        .cta-button:hover {{
            background-color: #45A049;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <h2>Hello , {order.UserName}</h2>
        <p>Thank you for your order ❤️. Below are the details of your order :</p>
        <div class='order-details'>
            <p><strong>Order Number:</strong> {order.Id}</p>
            <p><strong>Address:</strong> {order.AddressUser}</p>
            <p><strong>Phone:</strong> {order.PhoneNumberUser}</p>
            <p><strong>Order Time:</strong> {order.OrderTime}</p>
            <p><strong>Expected Delivery:</strong> {DateTime.Now.AddDays(5)}</p>
            <p><strong>Notes:</strong> {(string.IsNullOrEmpty(order.Notes) ? "No notes" : order.Notes)}</p>
        </div>
        <h3>Products:</h3>
        <ul class='product-list'>
            {string.Join("", products.Select(item => $"<li>{item.Product.Name}: {item.Product.Price} x {item.CountProduct} = {item.Product.Price * item.CountProduct} EGY</li>"))}
        </ul>
        <p class='total-price'>Total Price: {order.ToltaPrice} EGY</p>
        <p class='footer'>Thank you for trusting us😊<br/>Eng. Abdelhamid Elkawasy, CEO of El-Mohandes Tech</p>
    </div>
</body>
</html>";

                await _emailSender.SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to send the order confirmation email.", ex);
            }
        }


        private async Task SendOrderConfirmationEmailToAdminAsync(Order order, List<CartProduct> products, string toEmail)
        {
            try
            {
                string subject = "New Order Notification - El-MOHANDES COMPANY";
                string body = $@"
        <div style='font-family: Arial, sans-serif; color: #333; background-color: #f4f4f4; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                <h2 style='color: #2e6c80; text-align: center;'>New Order Notification</h2>
                <p style='font-size: 16px; color: #555;'>Dear Admin,</p>
                <p style='font-size: 16px; color: #555;'>A new order has been placed. Below are the details:</p>
                <table style='width: 100%; margin-bottom: 20px; border-collapse: collapse;'>
                   <tr style='background-color: #f0f0f0;'>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Order Id</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{order.Id}</td>
                    </tr>
                    <tr style='background-color: #f0f0f0;'>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Customer Name</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{order.UserName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Shipping Address</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{order.AddressUser}</td>
                    </tr>
                    <tr style='background-color: #f0f0f0;'>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Contact Phone</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{order.PhoneNumberUser}</td>
                    </tr>
                        <tr style='background-color: #f0f0f0;'>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Order Created On</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{order.OrderTime}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>maximum Delivery</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.Now.AddDays(5)}</td>
                    </tr>
                    <tr style='background-color: #f0f0f0;'>
                        <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Order Notes</td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{(string.IsNullOrEmpty(order.Notes) ? "No notes provided" : order.Notes)}</td>
                    </tr>
                </table>
                
                <h3 style='color: #2e6c80;'>Product Details</h3>
                <ul style='padding-left: 20px;'>";

                foreach (var item in products)
                {
                    body += $@"
                <li style='padding: 10px; border: 1px solid #ddd; margin-bottom: 10px; background-color: #f9f9f9; border-radius: 5px;'>
                    <strong>{item.Product.Name}</strong>: {item.Product.Price} x {item.CountProduct} = {(item.Product.Price * item.CountProduct)} EGY
                </li>";
                }

                body += $@"
                </ul>
                <p><strong>Total Price:</strong> {order.ToltaPrice} EGY</p>
                <p style='font-size: 14px; color: #555;'>For any queries regarding this order, please contact the customer directly or refer to the order management system for more details.</p>
                <p style='margin-top: 30px; font-size: 16px; color: #555;'>Best regards,</p>
                <p style='font-size: 16px; color: #555;'>
                    <strong>El-MOHANDES COMPANY</strong><br>
                    Order Management Team
                </p>
            </div>
        </div>";

                await _emailSender.SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to send the order confirmation email.", ex);
            }
        }

        private async Task SendOrderCancelledEmailToUserAsync(Order order, string toEmail, bool isArabic)
        {
            try
            {
                string productDetails = "";
                productDetails += isArabic ?
                    "<table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>"
                    + "<thead style='background-color: #f8f8f8; text-align: right;'>"
                    + "<tr>"
                    + "<th style='padding: 10px; border: 1px solid #ddd;'>اسم المنتج</th>"
                    + "<th style='padding: 10px; border: 1px solid #ddd;'>الكمية</th>"
                    + "</tr>"
                    + "</thead>"
                    + "<tbody>"
                    : "<table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>"
                    + "<thead style='background-color: #f8f8f8; text-align: left;'>"
                    + "<tr>"
                    + "<th style='padding: 10px; border: 1px solid #ddd;'>Product Name</th>"
                    + "<th style='padding: 10px; border: 1px solid #ddd;'>Quantity</th>"
                    + "</tr>"
                    + "</thead>"
                    + "<tbody>";

                foreach (var item in order.Items)
                {
                    productDetails += isArabic ?
                    $"<tr><td style='padding: 10px; border: 1px solid #ddd;'>{item.product.Name}</td><td style='padding: 10px; border: 1px solid #ddd;'>{item.CountOfProduct}</td></tr>"
                    : $"<tr><td style='padding: 10px; border: 1px solid #ddd;'>{item.product.Name}</td><td style='padding: 10px; border: 1px solid #ddd;'>{item.CountOfProduct}</td></tr>";
                }

                productDetails += "</tbody></table>";


                string subject = isArabic ? "إلغاء الطلب - شركة المهندس" : "Order Cancellation - El-MOHANDES COMPANY";
                string body = isArabic ? $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Arial', sans-serif;
            color: #333;
            background-color: #f4f4f4;
            padding: 20px;
            direction: rtl;
        }}
        .email-container {{
            background-color: #ffffff;
            border-radius: 10px;
            max-width: 600px;
            margin: auto;
            padding: 20px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }}
        h2 {{
            color: #E74C3C;
            font-size: 24px;
            margin-bottom: 20px;
        }}
        p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 15px;
        }}
        .order-details {{
            background-color: #f8f8f8;
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <h2>مرحبا , {order.UserName}</h2>
        <h4>لقد تم إلغاء طلبك ❌</h4>
        <p>نأسف لإبلاغك أن طلبك قد تم إلغاؤه. تفاصيل الطلب الملغى كالتالي:</p>
        <div class='order-details'>
            <p><strong>رقم الطلب:</strong> {order.Id}</p>
            <p><strong>العنوان:</strong> {order.AddressUser}</p>
            <p><strong>الهاتف:</strong> {order.PhoneNumberUser}</p>
            <p><strong>وقت الطلب:</strong> {order.OrderTime}</p>
            <p><strong>وقت الإلغاء:</strong> {DateTime.Now}</p>
            <p><strong>ملاحظات:</strong> {(string.IsNullOrEmpty(order.Notes) ? "لا توجد ملاحظات" : order.Notes)}</p>
        </div>
        <p><strong>تفاصيل المنتجات:</strong></p>
        {productDetails}
        <p><strong>إجمالي السعر:</strong> {order.ToltaPrice} جنيه مصري</p>
        <p>نأسف للإزعاج. إذا كنت بحاجة إلى أي مساعدة، لا تتردد في التواصل معنا.</p>
        <p>مع أطيب التحيات،</p>
        <p>المهندس عبد الحميد القوصي، الرئيس التنفيذي لشركة المهندس</p>
    </div>
</body>
</html>"
                : $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Arial', sans-serif;
            color: #333;
            background-color: #f4f4f4;
            padding: 20px;
        }}
        .email-container {{
            background-color: #ffffff;
            border-radius: 10px;
            max-width: 600px;
            margin: auto;
            padding: 20px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }}
        h2 {{
            color: #E74C3C;
            font-size: 24px;
            margin-bottom: 20px;
        }}
        p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 15px;
        }}
        .order-details {{
            background-color: #f8f8f8;
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <h2>Hello, {order.UserName}</h2>
        <h4>Your order has been canceled ❌</h4>
        <p>We're sorry to inform you that your order has been canceled. Here are the details of the canceled order:</p>
        <div class='order-details'>
            <p><strong>Order Number:</strong> {order.Id}</p>
            <p><strong>Address:</strong> {order.AddressUser}</p>
            <p><strong>Phone:</strong> {order.PhoneNumberUser}</p>
            <p><strong>Order Time:</strong> {order.OrderTime}</p>
            <p><strong>Cancellation Time:</strong> {DateTime.Now}</p>
            <p><strong>Notes:</strong> {(string.IsNullOrEmpty(order.Notes) ? "No notes" : order.Notes)}</p>
        </div>
        <p><strong>Product Details:</strong></p>
        {productDetails}
        <p><strong>Total Price:</strong> {order.ToltaPrice} EGY</p>
        <p>We apologize for the inconvenience. If you need any assistance, please don't hesitate to contact us.</p>
        <p>Best Regards,</p>
        <p>Eng. Abdelhamid Elkawasy, CEO of El-Mohandes Tech</p>
    </div>
</body>
</html>";

                await _emailSender.SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to send the order cancellation email.", ex);
            }
        }

        private async Task SendOrderCancelledEmailToAdminAsync(Order order, string toEmail)
        {
            try
            {
                string productDetails = "";
                foreach (var item in order.Items)
                {
                    productDetails += $"<tr><td style='padding: 10px; border: 1px solid #ddd;'>{item.product.Name}</td><td style='padding: 10px; border: 1px solid #ddd;'>{item.CountOfProduct}</td></tr>";
                }

                string subject = "Order Cancellation Notification - El-MOHANDES COMPANY";
                string body = $@"
<div style='font-family: Arial, sans-serif; color: #333; background-color: #f4f4f4; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <h2 style='color: #E74C3C; text-align: center;'>Order Cancellation Notification</h2>
        <p style='font-size: 16px; color: #555;'>Dear Admin,</p>
        <p style='font-size: 16px; color: #555;'>The following order has been canceled. Please see the details below:</p>
        <table style='width: 100%; margin-bottom: 20px; border-collapse: collapse;'>
            <tr style='background-color: #f0f0f0;'>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Order ID</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.Id}</td>
            </tr>
            <tr>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Customer Name</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.UserName}</td>
            </tr>
            <tr style='background-color: #f0f0f0;'>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Shipping Address</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.AddressUser}</td>
            </tr>
            <tr>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Contact Phone</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.PhoneNumberUser}</td>
            </tr>
            <tr style='background-color: #f0f0f0;'>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Order Time</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.OrderTime}</td>
            </tr>
            <tr>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Cancellation Time</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.Now}</td>
            </tr>
            <tr style='background-color: #f0f0f0;'>
                <td style='padding: 10px; font-weight: bold; border: 1px solid #ddd;'>Total Price</td>
                <td style='padding: 10px; border: 1px solid #ddd;'>{order.ToltaPrice} EGY</td>
            </tr>
        </table>
        <h4>Product Details:</h4>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr style='background-color: #f0f0f0;'>
                <th style='padding: 10px; border: 1px solid #ddd;'>Product Name</th>
                <th style='padding: 10px; border: 1px solid #ddd;'>Quantity</th>
            </tr>
            {productDetails}
        </table>
        <p>If you need any further information, please contact the support team.</p>
               <p style='margin-top: 30px; font-size: 16px; color: #555;'>Best regards,</p>
                <p style='font-size: 16px; color: #555;'>
                    <strong>El-MOHANDES COMPANY</strong><br>
                    Order Management Team
                </p>
    </div>
</div>";

                await _emailSender.SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to send the order cancellation email to the admin.", ex);
            }
        }




    }
}
