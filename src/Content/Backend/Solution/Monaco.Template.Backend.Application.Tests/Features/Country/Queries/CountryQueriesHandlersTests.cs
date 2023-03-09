﻿using FluentAssertions;
using Microsoft.Extensions.Primitives;
using MockQueryable.Moq;
using Monaco.Template.Backend.Application.DTOs;
using Monaco.Template.Backend.Application.Features.Country.Queries;
using Monaco.Template.Backend.Application.Infrastructure.Context;
using Monaco.Template.Backend.Common.Tests.Factories;
using Monaco.Template.Backend.Common.Tests.Factories.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monaco.Template.Backend.Application.Tests.Features.Country.Queries;

[ExcludeFromCodeCoverage]
[Trait("Application Queries", "Country Queries")]
public class CountryQueriesHandlersTests
{
	[Theory(DisplayName = "Get country list without params succeeds")]
	[AnonymousData]
	public async Task GetCountryListWithoutParamsSucceeds(List<Domain.Model.Country> countries)
	{
		var dbContextMock = SetupMock(countries);
		var query = new GetCountryListQuery(new List<KeyValuePair<string, StringValues>>());

		var sut = new CountryQueriesHandlers(dbContextMock.Object);
		var result = await sut.Handle(query, new CancellationToken());

		result.Should()
			  .HaveCount(countries.Count).And
			  .Contain(x => countries.Any(c => c.Name == x.Name)).And
			  .BeInAscendingOrder(x => x.Name);
	}

	[Theory(DisplayName = "Get country list with params succeeds")]
	[AnonymousData]
	public async Task GetCountryListWithParamsSucceeds(List<Domain.Model.Country> countries)
	{
		var dbContextMock = SetupMock(countries);
		var countriesSet = countries.GetRange(0, 2);
		var query = new GetCountryListQuery(new List<KeyValuePair<string, StringValues>>(new KeyValuePair<string, StringValues>[]
																						 {
																							 new(nameof(CountryDto.Name),
																								 new(countriesSet.Select(x => x.Name).ToArray())),
																							 new("sort", $"-{nameof(CountryDto.Name)}")
																						 }));

		var sut = new CountryQueriesHandlers(dbContextMock.Object);

		var result = await sut.Handle(query, new CancellationToken());

		result.Should()
			  .HaveCount(countriesSet.Count).And
			  .Contain(x => countriesSet.Any(c => c.Name == x.Name)).And
			  .BeInDescendingOrder(x => x.Name);
	}

	[Fact(DisplayName = "Get existing country by Id succeeds")]
	public async Task GetExistingCountryByIdSucceeds()
	{
		var countries = CountryFactory.CreateMany().ToList();
		var dbContextMock = SetupMock(countries);
		var country = countries.First();
		var query = new GetCountryByIdQuery(country.Id);

		var sut = new CountryQueriesHandlers(dbContextMock.Object);
		var result = await sut.Handle(query, new CancellationToken());

		result.Should().NotBeNull();
		result!.Name.Should().Be(country.Name);
	}

	[Fact(DisplayName = "Get non-existing country by Id fails")]
	public async Task GetNonExistingCountryByIdFails()
	{
		var countries = CountryFactory.CreateMany().ToList();
		var dbContextMock = SetupMock(countries);
		var query = new GetCountryByIdQuery(Guid.NewGuid());

		var sut = new CountryQueriesHandlers(dbContextMock.Object);
		var result = await sut.Handle(query, new CancellationToken());

		result.Should().BeNull();
	}

	private static Mock<AppDbContext> SetupMock(IEnumerable<Domain.Model.Country> countries)
	{
		var dbSetMock = countries.AsQueryable().BuildMockDbSet();
		var dbContextMock = new Mock<AppDbContext>();
		dbContextMock.Setup(x => x.Set<Domain.Model.Country>())
					 .Returns(dbSetMock.Object);

		return dbContextMock;
	}
}