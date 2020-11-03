using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using WebApplication.Models;
using WebApplication.Providers;
using WebApplication.Results;

namespace WebApplication.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
        private ApplicationDbContext DBContext;

        public AccountController()
        {
            DBContext = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }


        //[HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]

        // GET api/Account/UserInfo
        [HttpGet]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            //ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            return new UserInfoViewModel
            {
                //HasRegistered = externalLogin == null,
                //LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null,
                Login = user.UserName,
                FistName = user.FistName,
                SecondName = user.SecondName,
                Patronymic = user.Patronymic,
                DateOfBirth = user.DateOfBirth,
                Emails = user.Emails.Select(m => m.Value),
                PhoneNumbers = user.PhoneNumbers.Select(ph => ph.Value),
                Addresses = user.Addresses.Select(a =>
                    new { House = a.House, City = a.City, Street = a.Street, Apartment = a.Apartment }),
            };
        }

        #region Функции "из коробки"

        // POST api/Account/Logout
        [HttpPost]
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [HttpPost]
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [HttpPost]
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("Сбой внешнего входа.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("Внешнее имя входа уже связано с учетной записью.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [HttpPost]
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/RegisterExternal
        [HttpPost]
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Login, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            return Ok();
        }

        #endregion

        // POST api/Account/Register
        [HttpPost]
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            DateTime.TryParse(model.DateOfBirth, out DateTime dateofBirth);

            var user = new ApplicationUser()
            {
                UserName = model.Login,
                FistName = model.FistName,
                SecondName = model.SecondName,
                Patronymic = model.Patronymic,
                DateOfBirth = dateofBirth.ToString("d"),
                Email = model.Email
            };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            user.Emails = new List<UserEmail>() { new UserEmail() { Value = model.Email } };
            user.PhoneNumbers = new List<UserPhoneNumber> { new UserPhoneNumber() { Value = model.PhoneNumber } };
            user.Addresses = new List<UserAddress>
            {
                new UserAddress()
                {
                    City = model.Address.City,
                    Street = model.Address.Street,
                    House = model.Address.House,
                    Apartment = model.Address.Apartment
                }
            };

            result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/Email
        [HttpPost]
        [Route("Email")]
        public async Task<IHttpActionResult> AddEmail(EmailBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            if (user.Emails.Any(em => em.Value == model.Email))
                ModelState.AddModelError("Email", $"Почтовый адрес {model.Email} уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.Emails.Add(new UserEmail() { Value = model.Email });
            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/Email
        [HttpGet]
        [Route("Email")]
        public UserEmailViewModel GetEmail()
        {
            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            return new UserEmailViewModel
            {
                Emails = user.Emails.Select(m => m.Value)
            };
        }

        // PUT api/Account/Email
        [HttpPut]
        [Route("Email")]
        public async Task<IHttpActionResult> PutEmail(PutEmailBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserEmail existingEmail = user.Emails.FirstOrDefault(em => em.Value == model.OldEmail);

            if (existingEmail == null)
                ModelState.AddModelError("OldEmail", $"Почтовый адрес {model.OldEmail} не найден");

            if (user.Emails.Any(em => em.Value == model.NewEmail))
                ModelState.AddModelError("NewEmail", $"Почтовый адрес {model.NewEmail} уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.Emails.Remove(existingEmail);
            DBContext.Entry(existingEmail).State = EntityState.Deleted;
            user.Emails.Add(new UserEmail() { Value = model.NewEmail });

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // DELETE api/Account/Email
        [HttpDelete]
        [Route("Email")]
        public async Task<IHttpActionResult> DeleteEmail(EmailBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserEmail existingEmail = user.Emails.FirstOrDefault(em => em.Value == model.Email);

            if (existingEmail == null)
                ModelState.AddModelError("Email", $"Почтовый адрес {model.Email} не найден");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            user.Emails.Remove(existingEmail);
            DBContext.Entry(existingEmail).State = EntityState.Deleted;

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/PhoneNumber
        [HttpPost]
        [Route("PhoneNumber")]
        public async Task<IHttpActionResult> AddPhoneNumber(PhoneNumberBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            if (user.PhoneNumbers.Any(p => p.Value == model.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", $"Номер телефона {model.PhoneNumber} уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.PhoneNumbers.Add(new UserPhoneNumber() { Value = model.PhoneNumber });

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/PhoneNumber
        [HttpGet]
        [Route("PhoneNumber")]
        public UserPhoneNumberViewModel GetPhoneNumber()
        {
            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            return new UserPhoneNumberViewModel
            {
                PhoneNumbers = user.PhoneNumbers.Select(m => m.Value)
            };
        }

        // PUT api/Account/PhoneNumber
        [HttpPut]
        [Route("PhoneNumber")]
        public async Task<IHttpActionResult> PutPhoneNumber(PutPhoneNumberBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserPhoneNumber existingPhoneNumber = user.PhoneNumbers.FirstOrDefault(ph => ph.Value == model.OldPhoneNumber);

            if (existingPhoneNumber == null)
                ModelState.AddModelError("OldEmail", $"Номер телефона {model.OldPhoneNumber} не найден");

            if (user.PhoneNumbers.Any(ph => ph.Value == model.NewPhoneNumber))
                ModelState.AddModelError("NewEmail", $"Номер телефона {model.NewPhoneNumber} уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.PhoneNumbers.Remove(existingPhoneNumber);
            DBContext.Entry(existingPhoneNumber).State = EntityState.Deleted;
            user.PhoneNumbers.Add(new UserPhoneNumber() { Value = model.NewPhoneNumber });

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // DELETE api/Account/PhoneNumber
        [HttpDelete]
        [Route("PhoneNumber")]
        public async Task<IHttpActionResult> DeletePhoneNumber(PhoneNumberBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserPhoneNumber existingPhoneNumber = user.PhoneNumbers.FirstOrDefault(ph => ph.Value == model.PhoneNumber);

            if (existingPhoneNumber == null)
                ModelState.AddModelError("PhoneNumber", $"Номер телефона {model.PhoneNumber} не найден");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.PhoneNumbers.Remove(existingPhoneNumber);
            DBContext.Entry(existingPhoneNumber).State = EntityState.Deleted;

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/Address
        [HttpPost]
        [Route("Address")]
        public async Task<IHttpActionResult> AddAddress(AddressBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserAddress address = new UserAddress()
            {
                City = model.City,
                Street = model.Street,
                House = model.House,
                Apartment = model.Apartment
            };

            if (user.Addresses.Any(a => a.Equals(address)))
                ModelState.AddModelError("Address",
                    $"Адрес гор. {address.City}, " +
                    $"ул. {address.Street}, " +
                    $"дом {address.House}" +
                    (address.Apartment == "" ? "" : $", кв. {address.Apartment}")
                    + " уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.Addresses.Add(address);

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/Address
        [HttpGet]
        [Route("Address")]
        public UserAddressViewModel GetAddress()
        {
            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            return new UserAddressViewModel
            {
                Addresses = user.Addresses.Select(a =>
                    new { House = a.House, City = a.City, Street = a.Street, Apartment = a.Apartment })
            };
        }

        // PUT api/Account/Address
        [HttpPut]
        [Route("Address")]
        public async Task<IHttpActionResult> PutAddress(PutAddressBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserAddress oldAddress = new UserAddress()
            {
                City = model.OldAddress.City,
                Street = model.OldAddress.Street,
                House = model.OldAddress.House,
                Apartment = model.OldAddress.Apartment
            };

            UserAddress newAddress = new UserAddress()
            {
                City = model.NewAddress.City,
                Street = model.NewAddress.Street,
                House = model.NewAddress.House,
                Apartment = model.NewAddress.Apartment
            };

            UserAddress existingAddress = user.Addresses.FirstOrDefault(ad => ad.Equals(oldAddress));

            if (existingAddress == null)
                ModelState.AddModelError("OldAddress",
                    $"Адрес гор. {oldAddress.City}, " +
                    $"ул. {oldAddress.Street}, " +
                    $"дом {oldAddress.House}" +
                    (oldAddress.Apartment == "" ? "" : $", кв. {oldAddress.Apartment}") +
                    " не найден");

            if (user.Addresses.Any(ad => ad.Equals(newAddress)))
                ModelState.AddModelError("NewAddress",
                    $"Адрес гор. {newAddress.City}, " +
                    $"ул. {newAddress.Street}, " +
                    $"дом {newAddress.House}" +
                    (newAddress.Apartment == "" ? "" : $", кв. {newAddress.Apartment}") +
                    " уже существует");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.Addresses.Remove(existingAddress);
            DBContext.Entry(existingAddress).State = EntityState.Deleted;
            user.Addresses.Add(newAddress);

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // DELETE api/Account/Address
        [HttpDelete]
        [Route("Address")]
        public async Task<IHttpActionResult> DeleteAddress(AddressBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            if (user == null)
                return null;

            UserAddress address = new UserAddress()
            {
                City = model.City,
                Street = model.Street,
                House = model.House,
                Apartment = model.Apartment
            };

            UserAddress existingAddress = user.Addresses.FirstOrDefault(ad => ad.Equals(address));

            if (existingAddress == null)
                ModelState.AddModelError("Address",
                    $"Адрес гор. {address.City}, " +
                    $"ул. {address.Street}, " +
                    $"дом {address.House}" +
                    (address.Apartment == "" ? "" : $", кв. {address.Apartment}") +
                    " не найден");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user.Addresses.Remove(existingAddress);
            DBContext.Entry(existingAddress).State = EntityState.Deleted;

            IdentityResult result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Вспомогательные приложения

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // Ошибки ModelState для отправки отсутствуют, поэтому просто возвращается пустой BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("Значение strengthInBits должно нацело делиться на 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        //try
        //{
        //   
        //   
        //}
        //catch (DbEntityValidationException ex)
        //{
        //    foreach (DbEntityValidationResult validationError in ex.EntityValidationErrors)
        //    {
        //        var qwe = "Object: " + validationError.Entry.Entity.ToString();

        //        foreach (DbValidationError err in validationError.ValidationErrors)
        //        {
        //            qwe += "\n" + err.ErrorMessage;
        //        }
        //        var f = false;
        //    }
        //}

        #endregion
    }
}
