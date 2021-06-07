using Microsoft.EntityFrameworkCore;
using Slacek.Server.Models;
using System;

namespace Slacek.Server
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "database";
            string database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "slacek";
            string user = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "slacek";
            string password = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "slacek";
            options.UseSqlite(@"Data Source=./slacek.db");
        }
    }
}
