using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TraspasoDeCartera.Data;
using TraspasoDeCartera.DataAccess;

public class DataRepository
{
    private readonly string _connectionString;

    public DataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }


    public (bool exists, string? errorMessage) ExistsData(TraspasoDeCartera.Data.ExcelRow excelRow)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            bool opportunityExists = CheckCreditExists(connection, excelRow.DNICliente, excelRow.Solicitud);
            if (!opportunityExists)
            {
                return (false, "Credit no encontrado");
            }

            bool executiveExists = ExecutiveExists(connection,excelRow.DNIEjecutivoAdmin, excelRow.NombreEjecutivoAdmin);

            if (!executiveExists)
            {
                return (false, "Ejecutivo no encontrado.");
            }
            return (true, null);

    }
}


public List<DatabaseRow> RetrieveClientDatabaseRows(string dniCliente)
{
    var databaseRows = new List<DatabaseRow>();

    using (var connection = new SqlConnection(_connectionString))
    {
        connection.Open();
        using (var command = new SqlCommand("SELECT CR.pnet_OpportunityNumber, C.pnet_DocumentNumber, S.pnet_ExcecutiveID, S.FullName FROM CRM.pnet_CreditBase CR INNER JOIN CRM.ContactBase C ON CR.pnet_ContactId = C.ContactId INNER JOIN CRM.SystemUserBase S ON CR.pnet_CommercialExecutiveAdminId = S.SystemUserId WHERE C.pnet_DocumentNumber = @DNICliente", connection))
        {
            command.Parameters.AddWithValue("@DNICliente", dniCliente);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var databaseRow = new DatabaseRow
                    {
                        Solicitud = reader["pnet_OpportunityNumber"].ToString(),
                        DNICliente = reader["pnet_DocumentNumber"].ToString(),
                        DNIEjecutivoAdmin = reader["pnet_ExcecutiveID"].ToString(),
                        NombreEjecutivoAdmin = reader["FullName"].ToString()
                    };
                    databaseRows.Add(databaseRow);
                }
            }
        }
    }

    return databaseRows;
}

private bool CheckCreditExists(SqlConnection connection, string dniCliente, string solicitud)
{
        string tipCta = "330";
        string sitCont = "Vigente";
        using (var command = new SqlCommand("SELECT COUNT(*) FROM CRM.pnet_creditBase CR INNER JOIN CRM.ContactBase C ON CR.pnet_ContactId = C.ContactId INNER JOIN vw_StatusClientes S ON S.NIF=CR.PNET_NIF  WHERE C.pnet_DocumentNumber = @DNICliente AND CR.pnet_OpportunityNumber = @Solicitud AND S.[TIP CTA]=@TipCta AND S.[SIT CONT]=@SitCont", connection))
    {
            command.Parameters.AddWithValue("@DNICliente", dniCliente);
            command.Parameters.AddWithValue("@Solicitud", solicitud);
            command.Parameters.AddWithValue("@TipCta", tipCta);
            command.Parameters.AddWithValue("@SitCont", sitCont);
            return (int)command.ExecuteScalar() > 0;
    }
}
public bool ExecutiveExists(SqlConnection connection,string executiveId, string executiveName)
{
        using (var command = new SqlCommand("SELECT COUNT(*) FROM CRM.SystemUserBase WHERE pnet_ExcecutiveID = @ExecutiveId AND FullName=@ExecutiveName AND IsDisabled=0", connection))
        {
            command.Parameters.AddWithValue("@ExecutiveId", executiveId);
            command.Parameters.AddWithValue("@ExecutiveName", executiveName);
            int count = (int)command.ExecuteScalar();

            return count > 0;
        }
    }
}

