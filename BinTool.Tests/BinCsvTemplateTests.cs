using System.Text;
using BinTool.Application.Services;
using FluentAssertions;

namespace BinTool.Tests;

// The template is only worth handing out if the importer accepts it back unedited, so that is what
// these check: the file the page downloads is run through the real import.
public class BinCsvTemplateTests : ImportTestBase
{
    [Fact]
    public async Task The_template_imports_as_it_is_downloaded()
    {
        var result = await RunBytes(BinCsvTemplate.ToCsvBytes(), BinCsvTemplate.FileName);

        result.TotalRows.Should().Be(1);
        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
        result.ConflictCount.Should().Be(0);
    }

    [Fact]
    public void The_header_names_every_column_the_importer_reads()
    {
        var header = Encoding.UTF8.GetString(BinCsvTemplate.ToCsvBytes())
            .TrimStart('﻿')
            .Split("\r\n")[0];

        header.Should().Be(Header);
    }
}
