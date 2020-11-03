using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using WebApplication.Areas.HelpPage.ModelDescriptions;

namespace WebApplication.Models
{
    // Модели, используемые в качестве параметров действий AccountController.

    public class AddExternalLoginBindingModel
    {
        [Required]
        [Display(Name = "Внешний маркер доступа")]
        public string ExternalAccessToken { get; set; }
    }

    public class ChangePasswordBindingModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Значение {0} должно содержать не менее {2} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение нового пароля")]
        [Compare("NewPassword", ErrorMessage = "Новый пароль и его подтверждение не совпадают.")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterBindingModel
    {
        [Required]
        [Display(Name = "Login пользователя")]
        public string Login { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Значение {0} должно содержать не менее {2} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароль и его подтверждение не совпадают.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Имя пользователя")]
        public string FistName { get; set; }

        [Required]
        [Display(Name = "Фамилия пользователя")]
        public string SecondName { get; set; }

        [Display(Name = "Отчество пользователя")]
        public string Patronymic { get; set; }

        [Required]
        [DateOfBirthValidation(Name = "Дата рождения")]
        public string DateOfBirth { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Адрес электронной почты")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Номер телефона")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Адрес пользователя")]
        public AddressBindingModel Address { get; set; }
    }

    public class EmailBindingModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Адрес электронной почты")]
        public string Email { get; set; }
    }

    public class PutEmailBindingModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Старый адрес электронной почты")]
        public string OldEmail { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Новый адрес электронной почты")]
        public string NewEmail { get; set; }
    }

    public class PhoneNumberBindingModel
    {
        [Required]
        [Phone]
        [Display(Name = "Номер телефона")]
        public string PhoneNumber { get; set; }
    }

    public class PutPhoneNumberBindingModel
    {
        [Required]
        [Phone]
        [Display(Name = "Старый номер телефона")]
        public string OldPhoneNumber { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Новый номер телефона")]
        public string NewPhoneNumber { get; set; }
    }

    public class AddressBindingModel
    {
        [Required]
        [Display(Name = "Город")]
        public string City { get; set; }

        [Required]
        [Display(Name = "Улица")]
        public string Street { get; set; }

        [Required]
        [Display(Name = "Дом")]
        public string House { get; set; }

        [Display(Name = "Квартира")]
        public string Apartment { get; set; }
    }

    public class PutAddressBindingModel
    {
        [Required]
        [Display(Name = "Старый адрес пользователя")]
        public AddressBindingModel OldAddress { get; set; }

        [Required]
        [Display(Name = "Новый адрес пользователя")]
        public AddressBindingModel NewAddress { get; set; }
    }

    public class RegisterExternalBindingModel
    {
        [Required]
        [Display(Name = "Login пользователя")]
        public string Login { get; set; }

        [Required]
        [Display(Name = "Email пользователя")]
        public string Email { get; set; }
    }

    public class RemoveLoginBindingModel
    {
        [Required]
        [Display(Name = "Поставщик входа")]
        public string LoginProvider { get; set; }

        [Required]
        [Display(Name = "Ключ поставщика")]
        public string ProviderKey { get; set; }
    }

    public class SetPasswordBindingModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "Значение {0} должно содержать не менее {2} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение нового пароля")]
        [Compare("NewPassword", ErrorMessage = "Новый пароль и его подтверждение не совпадают.")]
        public string ConfirmPassword { get; set; }
    }

    public class DateOfBirthValidationAttribute : ValidationAttribute
    {
        public string Name;

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            if (!DateTime.TryParse(value.ToString(), out DateTime d))
            {
                if (ErrorMessage == null)
                    this.ErrorMessage = $"Поле {this.Name} содержит недопустимую дату";
                return false;
            }
            return true;
        }
    }
}
