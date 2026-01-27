using System.Collections.Generic;

namespace TransitManager.Core.DTOs.Settings
{
    public class CompanyProfileDto
    {
        public string CompanyName { get; set; } = "Hippocampe Import-Export";
        public string Address { get; set; } = "7 RUE PASCAL";
        public string ZipCode { get; set; } = "33370";
        public string City { get; set; } = "TRESSES";
        public string Country { get; set; } = "France";
        public string Email { get; set; } = "contact@hippocampeimportexport.com";
        public string Phone { get; set; } = "09.81.72.45.40";
        public string Mobile { get; set; } = "06 99 56 93 58";
        public string Siret { get; set; } = "891909772";
        public string TvaNumber { get; set; } = "FR42891909772";
        public string Rcs { get; set; } = "BORDEAUX";
        public string LegalStatus { get; set; } = "SAS";
        public string LogoUrl { get; set; } = "images/logo.jpg";
        
        // Helper for PDF
        public string FullAddress => $"{Address}, {ZipCode} {City}, {Country}";
    }

    public class BillingSettingsDto
    {
        public int DefaultQuoteValidityDays { get; set; } = 30;
        public string DefaultPaymentTerms { get; set; } = "Comptant";
        public string DefaultFooterNote { get; set; } = "SOUS RESERVE DU VOLUME FINAL RECEPTIONNE A QUAI -\nSOUS RESERVE DU PRIX FINAL LORS DES ACHATS EN MAGASIN -\nSOUS RESERVE DE PLACE SUR LE NAVIRE ET DES CONTENEURS DISPONIBLES -\nNOS DEVIS SONT SOUMIS A L'EVOLUTION MENSUEL DU PRIX DU GASOIL";
        public bool ShowGeneralConditions { get; set; } = false;
        public string GeneralConditionsText { get; set; } = "";
        
        // Quote specific defaults
        public string DefaultQuoteTitle { get; set; } = "DEVIS";
        public string DefaultCustomMessage { get; set; } = "";
    }

    public class BankDetailsDto
    {
        public string BankName { get; set; } = "CREDIT AGRICOLE D'AQUITAINE";
        public string Iban { get; set; } = "FR76 1330 6000 1923 1049 2210 473";
        public string Bic { get; set; } = "AGRIFRPP833";
        public string AccountHolder { get; set; } = "HIPPOCAMPE IMPORT EXPORT";
        public bool IsDefault { get; set; } = true;
    }
    public class InvoiceSettingsDto
    {
        public string DefaultTitle { get; set; } = "FACTURE";
        public string DefaultMessage { get; set; } = "";
        public string DefaultFooterNote { get; set; } = "TVA non applicable- article 293 B du CGI"; 
        public string DefaultPaymentTerms { get; set; } = "30 jours";
        public int LatePaymentPenaltyPercent { get; set; } = 10;
        public bool ShowGeneralConditions { get; set; } = false;
        public string GeneralConditionsText { get; set; } = "";
        
        // UI Toggles
        public bool ShowClientRef { get; set; } = false;
        public bool ShowDiscount { get; set; } = true;
        public bool ShowPaymentTerms { get; set; } = true;
        public bool ShowLatePenalty { get; set; } = true;
    }
}
