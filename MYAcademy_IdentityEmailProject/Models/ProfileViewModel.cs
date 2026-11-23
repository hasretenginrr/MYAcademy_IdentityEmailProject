using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
    public string Surname { get; set; } = "";

    [StringLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir.")]
    public string? Description { get; set; }

    [BindNever] public string UserName { get; set; } = "";
    [BindNever] public string Email { get; set; } = "";
    [BindNever] public string? ImageUrl { get; set; }
}