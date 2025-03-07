using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum Role
    {
        [Display(Name = "Administrador")] Administrator,
        [Display(Name = "Almacenista")] Storekeeper,
        [Display(Name = "Facturación")] Billing,
        [Display(Name = "Auxiliar Facturación")] BillingAssistant,
        [Display(Name = "Ventas")] Sales,
        [Display(Name = "Auxiliar Administrador")] AdministratorAssistant,
        [Display(Name = "Administrador Comercial")] AdministratorCommercial,
        [Display(Name = "Entradas Efectivo")] CashIncomes,
        [Display(Name = "Repartidor")] Distributor
    }
}