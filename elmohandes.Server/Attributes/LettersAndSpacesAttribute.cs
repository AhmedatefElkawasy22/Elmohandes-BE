
using System.Text.RegularExpressions;

public class LettersAndSpacesAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		if (value != null)
		{
			var name = value.ToString();
			if (Regex.IsMatch(name, @"^[a-zA-Z\u0600-\u06FF\s]+$"))
			{
				return ValidationResult.Success;
			}
		}
		return new ValidationResult("The Name field should only contain letters and spaces.");
	}
}
