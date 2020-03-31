﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurant.RestApi
{
    [ApiController, Route("[controller]")]
    public class ReservationsController
    {
        public ReservationsController(IReservationsRepository repository)
        {
            Repository = repository;
        }

        public IReservationsRepository Repository { get; }

        public async Task<ActionResult> Post(ReservationDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));
            if (!DateTime.TryParse(dto.At, out var d))
                return new BadRequestResult();
            if (dto.Email is null)
                return new BadRequestResult();
            if (dto.Quantity < 1)
                return new BadRequestResult();

            var r =
                new Reservation(d, dto.Email, dto.Name ?? "", dto.Quantity);
            await Repository.Create(r).ConfigureAwait(false);

            return new NoContentResult();
        }
    }
}