﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurant.RestApi
{
    public sealed class MaitreD
    {
        public MaitreD(params Table[] tables) : this(tables.AsEnumerable())
        {
        }

        public MaitreD(IEnumerable<Table> tables)
        {
            Tables = tables;
        }

        public IEnumerable<Table> Tables { get; }

        public bool WillAccept(
            IEnumerable<Reservation> existingReservations,
            Reservation candidate)
        {
            if (existingReservations is null)
                throw new ArgumentNullException(nameof(existingReservations));
            if (candidate is null)
                throw new ArgumentNullException(nameof(candidate));

            var relevantReservations =
                existingReservations.Where(candidate.Overlaps);

            var availableTables = Allocate(relevantReservations);

            return availableTables.Any(t => candidate.Quantity <= t.Seats);
        }

        private IEnumerable<Table> Allocate(
            IEnumerable<Reservation> reservations)
        {
            List<Table> availableTables = Tables.ToList();
            foreach (var r in reservations)
            {
                var table = availableTables.Find(t => r.Quantity <= t.Seats);
                if (table is { })
                {
                    availableTables.Remove(table);
                    if (table.IsCommunal)
                        availableTables.Add(table.Reserve(r.Quantity));
                }
            }

            return availableTables;
        }
    }
}