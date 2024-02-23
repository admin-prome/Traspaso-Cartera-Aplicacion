namespace TraspasoDeCartera.Data
{
    public class ExcelRow
    {
        public string Solicitud { get; set; }
        public string DNICliente { get; set; }
        public string Aceptado { get; set; }
        public string NombreEjecutivoAdmin { get; set; }
        public string DNIEjecutivoAdmin { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
    }
}
