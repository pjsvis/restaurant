﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurant.RestApi.Tests
{
    public sealed class SelfHostedApi : WebApplicationFactory<Startup>
    {
        private bool authorizeClient;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReservationsRepository>();
                services.AddSingleton<IReservationsRepository>(
                    new FakeDatabase());
            });
        }

        public async Task<HttpResponseMessage> GetRestaurant(string name)
        {
            var client = CreateClient();

            var homeResponse =
                await client.GetAsync(new Uri("", UriKind.Relative));
            homeResponse.EnsureSuccessStatusCode();
            var homeRepresentation =
                await homeResponse.ParseJsonContent<HomeDto>();
            var restaurant =
                homeRepresentation.Restaurants.First(r => r.Name == name);
            var address = restaurant.Links.FindAddress("urn:restaurant");

            return await client.GetAsync(address);
        }

        public async Task<HttpResponseMessage> PostReservation(
            string name,
            object reservation)
        {
            string json = JsonSerializer.Serialize(reservation);
            using var content = new StringContent(json);
            content.Headers.ContentType.MediaType = "application/json";

            var resp = await GetRestaurant(name);
            resp.EnsureSuccessStatusCode();
            var rest = await resp.ParseJsonContent<RestaurantDto>();
            var address = rest.Links.FindAddress("urn:reservations");

            return await CreateClient().PostAsync(address, content);
        }

        internal void AuthorizeClient()
        {
            authorizeClient = true;
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (!authorizeClient)
                return;

            var token = GenerateJwtToken();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private static string GenerateJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(
                "This is not the secret used in production.");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject =
                    new ClaimsIdentity(new[] { new Claim("role", "MaitreD") }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<HttpResponseMessage> PutReservation(
           Uri address,
           object reservation)
        {
            string json = JsonSerializer.Serialize(reservation);
            using var content = new StringContent(json);
            content.Headers.ContentType.MediaType = "application/json";
            return await CreateClient().PutAsync(address, content);
        }

        public async Task<HttpResponseMessage> GetCurrentYear(string name)
        {
            var resp = await GetRestaurant(name);
            resp.EnsureSuccessStatusCode();
            var rest = await resp.ParseJsonContent<RestaurantDto>();
            var address = rest.Links.FindAddress("urn:year");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetYear(string name, int year)
        {
            var resp = await GetCurrentYear(name);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();
            if (dto.Year == year)
                return resp;

            var rel = dto.Year < year ? "next" : "previous";

            var client = CreateClient();
            do
            {
                var address = dto.Links.FindAddress(rel);
                resp = await client.GetAsync(address);
                resp.EnsureSuccessStatusCode();
                dto = await resp.ParseJsonContent<CalendarDto>();
            } while (dto.Year != year);

            return resp;
        }

        public async Task<HttpResponseMessage> GetDay(
            string name,
            int year,
            int month,
            int day)
        {
            var resp = await GetYear(name, year);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();

            var target = new DateTime(year, month, day).ToIso8601DateString();
            var dayCalendar = dto.Days.Single(d => d.Date == target);
            var address = dayCalendar.Links.FindAddress("urn:day");
            return await CreateClient().GetAsync(address);
        }

        internal async Task<HttpResponseMessage> GetSchedule(
            string name,
            int year,
            int month,
            int day)
        {
            var resp = await GetYear(name, year);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();

            var target = new DateTime(year, month, day).ToIso8601DateString();
            var dayCalendar = dto.Days.Single(d => d.Date == target);
            var address = dayCalendar.Links.FindAddress("urn:schedule");
            return await CreateClient().GetAsync(address);
        }
    }
}
