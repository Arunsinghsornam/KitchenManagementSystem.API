using System;
using System.ComponentModel.DataAnnotations;

namespace KitchenManagementSystem.API.DTOs
{
    public class CreateSupplierDto
    {
        public Guid OutletId { get; set; }

        public string Name { get; set; }
        public string ContactPerson { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
        public string Mobile { get; set; }
        public string GstNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public decimal Outstanding { get; set; }
    }
}