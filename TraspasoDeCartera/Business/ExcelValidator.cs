using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        var excelRows = ConvertExcelToRows(workbook.Worksheet(1));
        var validateRows = new List<ExcelRow>(excelRows);

        ValidateRows(validateRows);
        ValidateClientGroups(validateRows);

        int errors = validateRows.Count(row => row.HasError);
        return (errors, validateRows);
    }

    private void ValidateRows(List<ExcelRow> rows)
    {
        foreach (var excelRow in rows)
        {
            ValidateExcelRow(excelRow);
        }
    }

    private void ValidateExcelRow(ExcelRow excelRow)
    {
        var (exists, errorMessage) = _repository.ExistsData(excelRow);

        if (!exists)
        {
            excelRow.HasError = true;
            excelRow.ErrorMessage = errorMessage;
        }
    }

    private void ValidateClientGroups(List<ExcelRow> rows)
    {
        foreach (var clientGroup in rows.GroupBy(row => row.DNICliente))
        {
            ValidateClientGroup(clientGroup, rows);
        }
    }

    private void ValidateClientGroup(IGrouping<string, ExcelRow> clientGroup, List<ExcelRow> validatedRows)
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
                return;
            }
        }

        if (!CheckSingleExecutive(clientGroup))
        {
            foreach (var excelRow in clientGroup)
            {
                excelRow.HasError = true;
                excelRow.ErrorMessage = "El cliente tiene múltiples ejecutivos dentro del excel";
            }
        }

        string dniEjecutivo = clientGroup.First().DNIEjecutivoAdmin;
        string dniCliente = clientGroup.Key;
        var databaseRows = _repository.RetrieveClientDatabaseRows(dniCliente);

        IdentifyMissingInExcel(clientGroup, databaseRows, dniEjecutivo);
        IdentifyMissingInDatabase(clientGroup, databaseRows);
    }

    private bool CheckSingleExecutive(IEnumerable<ExcelRow> excelRows)
    {
        var executiveIds = excelRows.Select(row => row.DNIEjecutivoAdmin).Distinct().ToList();
        return executiveIds.Count <= 1;
    }

    private void IdentifyMissingInExcel(IGrouping<string, ExcelRow> clientGroup, List<DatabaseRow> databaseRows, string dniEjecutivo)
    {
        var missingInExcel = databaseRows.Where(dbRow => !clientGroup.Any(excelRow => excelRow.Solicitud == dbRow.Solicitud) && dbRow.DNIEjecutivoAdmin != dniEjecutivo);
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
    }

    private void IdentifyMissingInDatabase(IGrouping<string, ExcelRow> clientGroup, List<DatabaseRow> databaseRows)
    {
        var missingInDatabase = clientGroup.Where(excelRow => !databaseRows.Any(dbRow => excelRow.Solicitud == dbRow.Solicitud));
        foreach (var missingRow in missingInDatabase)
        {
            missingRow.HasError = true;
            missingRow.ErrorMessage = "Fila de Excel no encontrada en la base de datos";
        }
    }

    private List<ExcelRow> ConvertExcelToRows(IXLWorksheet worksheet)
    {
        List<ExcelRow> rows = new List<ExcelRow>();
        int solicitudIndex = 1;
        int dniClienteIndex = 2;
        int aceptadoIndex = 3;
        int nombreEjecutivoAdminIndex = 4;
        int dniEjecutivoAdminIndex = 5;

        bool isFirstRow = true;

        foreach (var row in worksheet.RowsUsed())
        {
            //header
            if (isFirstRow)
            {
                isFirstRow = false;
                continue;
            }

            //CultureInfo. Because it was changing the ',' to '.'. 
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
