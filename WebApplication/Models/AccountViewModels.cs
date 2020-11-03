using System;
using System.Collections.Generic;

namespace WebApplication.Models
{
    // Модели, возвращаемые действиями AccountController.

    public class ExternalLoginViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string State { get; set; }
    }

    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; }

        public string Email { get; set; }

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
    }

    public class UserInfoViewModel
    {
        //public bool HasRegistered { get; set; }

        //public string LoginProvider { get; set; }

        public string Login { get; set; }

        public string FistName { get; set; }

        public string SecondName { get; set; }

        public string Patronymic { get; set; }

        public string DateOfBirth { get; set; }

        public IEnumerable<string> Emails { get; set; }

        public IEnumerable<string> PhoneNumbers { get; set; }

        public IEnumerable<dynamic> Addresses { get; set; }
    }

    public class UserEmailViewModel
    {
        public IEnumerable<string> Emails { get; set; }
    }

    public class UserPhoneNumberViewModel
    {
        public IEnumerable<string> PhoneNumbers { get; set; }
    }

    public class UserAddressViewModel
    {
        public IEnumerable<dynamic> Addresses { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }
}
