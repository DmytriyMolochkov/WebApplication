using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace WebApplication.Models
{
    // В профиль пользователя можно добавить дополнительные данные, если указать больше свойств для класса ApplicationUser. Подробности см. на странице https://go.microsoft.com/fwlink/?LinkID=317594.
    public class ApplicationUser : IdentityUser
    {
        public string FistName { get; set; }
        public string SecondName { get; set; }
        public string Patronymic { get; set; }
        public string DateOfBirth { get; set; }
        public virtual ICollection<UserEmail> Emails { get; set; }
        public virtual ICollection<UserPhoneNumber> PhoneNumbers { get; set; }
        public virtual ICollection<UserAddress> Addresses { get; set; }

        [NotMapped] public override bool EmailConfirmed { get; set; }
        [NotMapped] public override string PhoneNumber { get; set; }
        [NotMapped] public override bool PhoneNumberConfirmed { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Обратите внимание, что authenticationType должен совпадать с типом, определенным в CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Здесь добавьте настраиваемые утверждения пользователя
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<UserEmail> UserEmail { get; set; }
        public DbSet<UserPhoneNumber> UserPhoneNumber { get; set; }
        public DbSet<UserAddress> UserAddress { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
       
    }

    [Table("UserEmail")]
    public class UserEmail
    {
        [Key] public int ID { get; set; }
        [Required] public string UserID { get; set; }
        [Required] public string Value { get; set; }

        [InverseProperty("Emails")]
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }
    }

    [Table("UserPhoneNumber")]
    public class UserPhoneNumber
    {
        [Key] public int ID { get; set; }
        [Required] public string UserID { get; set; }
        [Required] public string Value { get; set; }

        [InverseProperty("PhoneNumbers")]
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }
    }

    [Table("UserAddress")]
    public class UserAddress
    {
        [Key] public int ID { get; set; }
        [Required] public string UserID { get; set; }
        [Required] public string City { get; set; }
        [Required] public string Street { get; set; }
        [Required] public string House { get; set; }
        public string Apartment { get; set; }

        [InverseProperty("Addresses")]
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        public override bool Equals(object obj)
        {
            UserAddress address = obj as UserAddress;

            if (address == null)
                return false;

            return 
                City == address.City &&
                Street == address.Street &&
                House == address.House &&
                Apartment == address.Apartment;
        }

        public override int GetHashCode()
        {
            return (City + Street + House + Apartment).GetHashCode();
        }
    }
}