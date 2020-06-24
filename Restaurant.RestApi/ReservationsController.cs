/* Copyright (c) Mark Seemann 2020. All rights reserved. */
﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurant.RestApi
{
    [ApiController, Route("reservations")]
    public class ReservationsController
    {
        public ReservationsController(
            IReservationsRepository repository,
            MaitreD maitreD)
        {
            Repository = repository;
            MaitreD = maitreD;
        }

        public IReservationsRepository Repository { get; }
        public MaitreD MaitreD { get; }

        [HttpPost]
        public async Task<ActionResult> Post(ReservationDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            Reservation? r = dto.Validate(Guid.NewGuid());
            if (r is null)
                return new BadRequestResult();

            var reservations = await Repository
                .ReadReservations(r.At)
                .ConfigureAwait(false);
            if (!MaitreD.WillAccept(DateTime.Now, reservations, r))
                return NoTables500InternalServerError();

            await Repository.Create(r).ConfigureAwait(false);

            return Reservation201Created(r);
        }

        private static ActionResult NoTables500InternalServerError()
        {
            return new ObjectResult("No tables available.")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        [SuppressMessage(
            "Globalization",
            "CA1305:Specify IFormatProvider",
            Justification = "Guids aren't culture-specific.")]
        private static ActionResult Reservation201Created(Reservation r)
        {
            return new CreatedAtActionResult(
                nameof(Get),
                null,
                new { id = r.Id.ToString("N") },
                null);
        }

        [SuppressMessage(
            "Globalization",
            "CA1305:Specify IFormatProvider",
            Justification = "ToString(\"o\") is already culture-neutral.")]
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(string id)
        {
            if (!Guid.TryParse(id, out var rid))
                return new NotFoundResult();

            Reservation? r =
                await Repository.ReadReservation(rid).ConfigureAwait(false);
            if (r is null)
                return new NotFoundResult();

            return new OkObjectResult(
                new ReservationDto
                {
                    Id = id,
                    At = r.At.ToString("o"),
                    Email = r.Email,
                    Name = r.Name,
                    Quantity = r.Quantity
                });
        }

        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            var rid = new Guid(id);
            await Repository.Delete(rid).ConfigureAwait(false);
        }
    }
}
