
public class ValidAddressAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		if (value == null)
		{
			return ValidationResult.Success;
		}

		var address = value.ToString();
		var regex = new Regex(@"^[a-zA-Z\u0600-\u06FF\s\-,/]*$");    

			if (regex.IsMatch(address))
		{
			return ValidationResult.Success;
		}

		return new ValidationResult("The Address field can only contain upper and lower case letters, spaces, and the characters '-', ',', '/'.");
	}
}
