﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using Ploeh.Samples.Restaurant.RestApi;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurant.RestApi
{
    public class SqlReservationsRepository : IReservationsRepository
    {
        public SqlReservationsRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public async Task Create(Reservation reservation)
        {
            if (reservation is null)
                throw new ArgumentNullException(nameof(reservation));

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(createReservationSql, conn);
            cmd.Parameters.Add(new SqlParameter("@Id", reservation.Id));
            cmd.Parameters.Add(new SqlParameter("@At", reservation.At));
            cmd.Parameters.Add(new SqlParameter("@Name", reservation.Name));
            cmd.Parameters.Add(new SqlParameter("@Email", reservation.Email));
            cmd.Parameters.Add(
                new SqlParameter("@Quantity", reservation.Quantity));

            await conn.OpenAsync().ConfigureAwait(false);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private const string createReservationSql = @"
            INSERT INTO [dbo].[Reservations] (
                [PublicId], [At], [Name], [Email], [Quantity])
            VALUES (@Id, @At, @Name, @Email, @Quantity)";

        public async Task<IReadOnlyCollection<Reservation>> ReadReservations(
            DateTime dateTime)
        {
            var result = new List<Reservation>();

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(readByRangeSql, conn);
            cmd.Parameters.AddWithValue("@at", dateTime.Date);

            await conn.OpenAsync().ConfigureAwait(false);
            using var rdr =
                await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (rdr.Read())
                result.Add(
                    new Reservation(
                        (Guid)rdr["PublicId"],
                        (DateTime)rdr["At"],
                        (string)rdr["Name"],
                        (string)rdr["Email"],
                        (int)rdr["Quantity"]));

            return result.AsReadOnly();
        }

        private const string readByRangeSql = @"
            SELECT [PublicId], [At], [Name], [Email], [Quantity]
            FROM [dbo].[Reservations]
            WHERE CONVERT(DATE, [At]) = @At";

        public async Task<Reservation?> ReadReservation(Guid id)
        {
            const string readByIdSql = @"
                SELECT [At], [Name], [Email], [Quantity]
                FROM [dbo].[Reservations]
                WHERE [PublicId] = @id";

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(readByIdSql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync().ConfigureAwait(false);
            using var rdr =
                await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (!rdr.Read())
                return null;

            return new Reservation(
                id,
                (DateTime)rdr["At"],
                (string)rdr["Name"],
                (string)rdr["Email"],
                (int)rdr["Quantity"]);
        }

        public async Task Delete(Guid id)
        {
            const string deleteSql = @"
                DELETE [dbo].[Reservations]
                WHERE [PublicId] = @id";

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(deleteSql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync().ConfigureAwait(false);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}
