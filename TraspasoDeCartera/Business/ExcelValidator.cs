using ClosedXML.Excel;
using System.Globalization;
using TraspasoDeCartera.Data;
using TraspasoDeCartera.DataAccess;
public class ExcelValidator
{
    private readonly DataRepository _repository;

    public ExcelValidator(DataRepository repository)
    {
        _repository = repository;
    }

    public (int errors, List<ExcelRow> validatedRows) ValidateExcel(IXLWorkbook workbook)
    {
        var worksheet = workbook.Worksheet(1);
        var excelRows = ConvertExcelToRows(worksheet);
        var validateRows = new List<ExcelRow>(excelRows); // List to store rows that need validation

        // Validate each Excel row individually
        foreach (var excelRow in validateRows)
        {
            ValidateExcelRow(excelRow);
        }

        // Validate client groups
        foreach (var clientGroup in validateRows.GroupBy(row => row.DNICliente))
        {
            if (!ValidateClientGroup(clientGroup, ref validateRows))
            {
                break; // Stop validation if an error occurs
            }
        }

        int errors = validateRows.Count(row => row.HasError);
        return (errors, validateRows);

    }

    private void ValidateExcelRow(ExcelRow excelRow)
    {
        var (exists, errorMessage) = _repository.ExistsData(excelRow);
        Console.WriteLine($"Checking data existence for Client: {excelRow.DNICliente}, Solicitud: {excelRow.Solicitud}, Exists: {exists}, ErrorMessage: {errorMessage}");

        if (!exists)
        {
            excelRow.HasError = true;
            excelRow.ErrorMessage = errorMessage;
        }
    }

    private bool ValidateClientGroup(IGrouping<string, ExcelRow> clientGroup, ref List<ExcelRow> validatedRows)
    {
        if (clientGroup.Any(x => x.HasError))
        {
            foreach (var excelRow in clientGroup)
            {
                excelRow.HasError = true;
                if (excelRow.ErrorMessage == null)
                {
                    excelRow.ErrorMessage = "Error con una fila del cliente";
                }
            }
            return false;
        }
        string dniCliente = clientGroup.Key;
        var databaseRows = _repository.RetrieveClientDatabaseRows(dniCliente);
        Console.WriteLine($"Retrieved {databaseRows.Count} database rows for Client: {dniCliente}");

        // Identify database rows missing in Excel
        var missingInExcel = databaseRows.Where(dbRow => !clientGroup.Any(excelRow => excelRow.Solicitud == dbRow.Solicitud));
        if (missingInExcel.Any())
        {
            foreach (var excelRow in clientGroup)
            {
                excelRow.HasError = true;
                if (excelRow.ErrorMessage == null)
                {
                    excelRow.ErrorMessage = "Faltan filas del cliente";
                }
            }
        }

        // Identify Excel rows missing in database
        var missingInDatabase = clientGroup.Where(excelRow => !databaseRows.Any(dbRow => excelRow.Solicitud == dbRow.Solicitud));
        foreach (var missingRow in missingInDatabase)
        {
            missingRow.HasError = true;
            missingRow.ErrorMessage = "Fila de Excel no encontrada en la base de datos";
        }

        // Check if each client has a single executive
        if (!CheckSingleExecutive(clientGroup))
        {
            // Error: Multiple executives associated with a client's opportunities
            foreach (var excelRow in clientGroup)
            {
                excelRow.HasError = true;
                excelRow.ErrorMessage = "El cliente tiene múltiples ejecutivos asociados después de la modificación";
            }
        }

        return true;
    }

    //    private bool MergeRows(List<ExcelRow> excelRows, List<DatabaseRow> databaseRows)
    //{
    //    foreach (var excelRow in excelRows)
    //    {
    //        var matchingDatabaseRows = databaseRows.Where(row => row.Solicitud == excelRow.Solicitud && row.DNICliente == excelRow.DNICliente).ToList();

    //        if (matchingDatabaseRows.Count == 0)
    //        {
    //            // This indicates an error, as there should be a corresponding database row for each Excel row
    //            Console.WriteLine($"No matching database row found for Excel row: {excelRow.Solicitud}, {excelRow.DNICliente}");
    //            return false;
    //        }

    //        // Update properties of database rows with Excel data
    //        foreach (var databaseRow in matchingDatabaseRows)
    //        {
    //            databaseRow.Solicitud = excelRow.Solicitud;
    //            databaseRow.DNICliente = excelRow.DNICliente;
    //            databaseRow.NombreEjecutivoAdmin = excelRow.NombreEjecutivoAdmin;
    //            databaseRow.DNIEjecutivoAdmin = excelRow.DNIEjecutivoAdmin;
    //        }
    //    }

    //    return true;
    //}

    private bool CheckSingleExecutive(IEnumerable<ExcelRow> excelRows)
    {
        var executiveIds = excelRows.Select(row => row.DNIEjecutivoAdmin).Distinct().ToList();
        return executiveIds.Count <= 1;
    }

    private List<ExcelRow> ConvertExcelToRows(IXLWorksheet worksheet)
    {
        List<ExcelRow> rows = new List<ExcelRow>();
        // Assuming the first row contains headers, adjust the indices accordingly
        int solicitudIndex = 1; // Assuming Solicitud is in the first column
        int dniClienteIndex = 2; // Assuming DNICliente is in the second column
        int aceptadoIndex = 3; // Assuming Aceptado is in the third column
        int nombreEjecutivoAdminIndex = 4; // Assuming NombreEjecutivoAdmin is in the fourth column
        int dniEjecutivoAdminIndex = 5; // Assuming DNIEjecutivoAdmin is in the fifth column

        // Skip the first row as it contains headers
        bool isFirstRow = true;

        foreach (var row in worksheet.RowsUsed())
        {
            if (isFirstRow)
            {
                isFirstRow = false;
                continue; // Skip processing headers
            }
            string solicitud = row.Cell(solicitudIndex).Value.ToString(new CultureInfo("es-ES"));
            string dniCliente = row.Cell(dniClienteIndex).Value.ToString();
            string aceptado = row.Cell(aceptadoIndex).Value.ToString();
            string nombreEjecutivoAdmin = row.Cell(nombreEjecutivoAdminIndex).Value.ToString();
            string dniEjecutivoAdmin = row.Cell(dniEjecutivoAdminIndex).Value.ToString();

            rows.Add(new ExcelRow
            {
                Solicitud = solicitud,
                DNICliente = dniCliente,
                Aceptado = aceptado,
                NombreEjecutivoAdmin = nombreEjecutivoAdmin,
                DNIEjecutivoAdmin = dniEjecutivoAdmin
            });
        }

        return rows;
    }
}
