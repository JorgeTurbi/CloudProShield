using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudShield.Commons;
public abstract class BaseAbstract
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public DateTime CreateAt { get; set; }
  public DateTime UpdateAt { get; set; }
}