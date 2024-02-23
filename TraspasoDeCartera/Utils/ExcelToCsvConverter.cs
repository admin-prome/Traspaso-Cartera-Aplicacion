using System.Collections.Generic;
using System.Text;
using TraspasoDeCartera.Data;

namespace TraspasoDeCartera.Utils
{

    public class ExcelToCsvConverter
    {
        public byte[] ConvertToCsv(List<ExcelRow> excelData)
        {
            StringBuilder csvBuilder = new StringBuilder();
            // Append CSV headers
            csvBuilder.AppendLine("Solicitud;DNICliente;Aceptado;NombreEjecutivoAdmin;DNIEjecutivoAdmin");
            // Append CSV data
            foreach (var row in excelData)
            {
                csvBuilder.AppendLine($"{row.Solicitud};{row.DNICliente};{row.Aceptado};{row.NombreEjecutivoAdmin};{row.DNIEjecutivoAdmin}");
            }
            return Encoding.UTF8.GetBytes(csvBuilder.ToString());
        }
    }
}
