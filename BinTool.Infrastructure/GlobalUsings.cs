// Every service and repository in this layer touches the entities, the abstractions it
// implements, and EF Core. Declaring them once keeps the per-file using blocks to the
// things that actually distinguish one file from another.
global using BinTool.Application.Abstractions;
global using BinTool.Domain.Entities;
global using BinTool.Domain.Services;
global using Microsoft.EntityFrameworkCore;
