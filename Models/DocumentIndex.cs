using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace DocSea.Models
{
    public class DocumentIndex
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string MainPath { get; set; }

        public string Status { get; set; }

        public int JobId { get; set; }
    }
}