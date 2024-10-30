using elmohandes.Server.Sevises;
using Microsoft.Extensions.Options;

namespace elmohandes.Server.UOW
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IUrlHelperService _urlHelperService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<SmtpSettings> _smtpSettings;
        private readonly UserManager<User> _userManager;
        public ProductRepository Product { get; }
        public GenricRepository<Brand> Brand { get; }
        public GenricRepository<Category> Category { get; }
        public CartRepository Cart { get; }
        public UserRepository User { get; }
        public OrderRepository Order { get; }



        public UnitOfWork(ApplicationDbContext context, IMapper mapper, IUrlHelperService urlHelperService, IHttpContextAccessor contextAccessor, IEmailSender emailSender, IOptions<SmtpSettings> smtpSettings, UserManager<User> userManager)
        {
            _context = context;
            _mapper = mapper;
            _urlHelperService = urlHelperService;
            _contextAccessor = contextAccessor;
            _emailSender = emailSender;
            _smtpSettings = smtpSettings;
            _userManager = userManager;
            Product = new ProductRepository(_context, _mapper, _urlHelperService);
            Brand = new GenricRepository<Brand>(_context);
            Category = new GenricRepository<Category>(_context);
            Cart = new CartRepository(_context, _contextAccessor);
            User = new UserRepository(_context, _contextAccessor,_emailSender,_userManager);
            Order = new OrderRepository(_context, _contextAccessor, _emailSender, _smtpSettings, this);
        }
    }
}
