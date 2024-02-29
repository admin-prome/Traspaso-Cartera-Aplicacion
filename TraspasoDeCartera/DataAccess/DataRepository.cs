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
                return (false, "Crédito no encontrado");
            }

            bool executiveExists = ExecutiveExists(connection, excelRow.DNIEjecutivoAdmin, excelRow.NombreEjecutivoAdmin);

            if (!executiveExists)
            {
                return (false, "Ejecutivo no encontrado.");
            }


            bool creditVigency = CheckCreditVigency(excelRow.Solicitud);

            if (!creditVigency)
            {
                return (false, "El crédito no está vigente.");
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
        using (var command = new SqlCommand("SELECT COUNT(*) FROM CRM.pnet_creditBase CR INNER JOIN CRM.ContactBase C ON CR.pnet_ContactId = C.ContactId WHERE C.pnet_DocumentNumber = @DNICliente AND CR.pnet_OpportunityNumber = @Solicitud", connection))
        {
            command.Parameters.AddWithValue("@DNICliente", dniCliente);
            command.Parameters.AddWithValue("@Solicitud", solicitud);
            return (int)command.ExecuteScalar() > 0;
        }
    }
    public bool ExecutiveExists(SqlConnection connection, string executiveId, string executiveName)
    {
        using (var command = new SqlCommand("SELECT COUNT(*) FROM CRM.SystemUserBase WHERE pnet_ExcecutiveID = @ExecutiveId AND FullName=@ExecutiveName AND IsDisabled=0", connection))
        {
            command.Parameters.AddWithValue("@ExecutiveId", executiveId);
            command.Parameters.AddWithValue("@ExecutiveName", executiveName);
            int count = (int)command.ExecuteScalar();

            return count > 0;
        }
    }
    public void InsertData(TraspasoDeCartera_Historial traspasoDeCartera_Historial)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(
                @"INSERT INTO TraspasoDeCartera_Historial (Email, CsvFile, DateUpdated)
                  VALUES (@Email, @CsvFile, @DateUpdated)", connection))
            {
                command.Parameters.AddWithValue("@Email", traspasoDeCartera_Historial.Email);
                command.Parameters.AddWithValue("@CsvFile", traspasoDeCartera_Historial.CsvFile);
                command.Parameters.AddWithValue("@DateUpdated", traspasoDeCartera_Historial.DateUpdated);
                command.ExecuteNonQuery();
            }
        }
    }

    public bool CheckCreditVigency(string solicitud)
    {
        string TIP_CTA = "330";
        string SIT_CONT = "VIGENTE";
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand("SELECT TOP(1) P.COLUMN21 FROM FTP_PADRONSUR P, CRM.pnet_CreditBase C WHERE P.COLUMN20 LIKE '%'+C.pnet_nif AND C.pnet_OpportunityNumber=@Solicitud AND P.COLUMN19 LIKE '%'+@TIP_CTA+'%'",connection))
            {
                command.Parameters.AddWithValue("@Solicitud", solicitud);
                command.Parameters.AddWithValue("@TIP_CTA", TIP_CTA);
                command.ExecuteNonQuery();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["COLUMN21"].ToString().Contains(SIT_CONT)) return true;
                    }
                    return false;
                }
            }
        }
    }
}

