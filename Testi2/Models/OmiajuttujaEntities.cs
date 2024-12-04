using System;
using System.Data.Entity;
using Testi2.Views;
using Testi2.Models;


namespace Testi2.Models
{
    // Entity Framework DbContext luokka
    public class OmiajuttujaEntities : DbContext
    {
        // Konstruktorin kautta annetaan yhteysmerkkijonon nimi
        public OmiajuttujaEntities() : base("name=OmiajuttujaEntities")
        {
        }

        // Määritellään taulut, joita käytetään sovelluksessa
        public DbSet<User> Users { get; set; }
    }

    // User-luokka, joka määrittelee käyttäjien ominaisuudet
    public class User
    {
        // Määritellään pääavain
        public int UserId { get; set; }  // Tämä on pääavain
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public bool? IsFirstLogin { get; set; }
        public string ResetToken { get; set; }
        public DateTime? TokenCreatedAt { get; set; }
    }
}
