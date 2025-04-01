using CloudShield.Commons;
using Entities.Users;

namespace CloudShield.Entities.Users;

public class Address : BaseAbstract
{

      public int UserId { get; set; }
      public int CountryId { get; set; }
      public int StateId { get; set; }
      public string City { get; set; }
      public string Street { get; set; }
      public string Line { get; set; }
      public string ZipCode { get; set; }
      public required virtual User User { get; set; }
      public required virtual Country Country { get; set; }
      public required virtual State State { get; set; }



}