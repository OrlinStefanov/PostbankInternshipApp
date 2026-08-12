// Controllers all authorize against the permission catalog and call the application
// abstractions, so those two come in globally rather than at the top of every file.
global using BinTool.Application.Abstractions;
global using BinTool.Application.Authorization;
global using BinTool.Domain.Common;
global using BinTool.Domain.Entities;
