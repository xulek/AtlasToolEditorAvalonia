# AtlasToolEditor

AtlasToolEditor is an Avalonia-based, cross-platform application (targeting .NET 9.0) for creating and arranging texture regions (or "atlas" regions) in a single image. The tool lets you define rectangular regions within an image, export them to a JSON file, and later arrange those regions on a fixed 1280×720 layout—useful for game development, UI design, or other graphics-related tasks.

## Features

- **Load an Image**: Import a large image (e.g., a spritesheet or texture atlas).
- **Define Regions**: Draw, move, and resize rectangular regions within the image. Each region includes:
  - A name
  - X, Y coordinates in the image
  - Width and height
- **Modify Regions:**
  - **Delete a Region**: Select a region and press **DELETE** to remove it.
  - **Rename a Region**: Double-click on a region to open an input dialog and edit its name.
- **Save to JSON**: Export all defined regions to a JSON file for later use or sharing.
- **Load Regions from JSON**: Import previously defined regions onto the same or a compatible image.
- **Arrange Regions**: A separate window (**ArrangementWindow**) allows you to:
  - Load a full image and a JSON file defining the regions
  - Zoom and pan within a fixed 1280×720 "arrangement area"
  - Drag and position each region on the layout
  - Save the final on-screen arrangement to another JSON file with updated positions

## Requirements

- [.NET 9.0 SDK](https://dotnet.microsoft.com/) (or later)
- [Avalonia UI](https://docs.avaloniaui.net/) framework
- A compatible IDE such as [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with C# extensions

## Getting Started

1. **Clone or Download** this repository.
2. **Open the Solution** in Visual Studio or your preferred editor, or navigate to the project folder in a terminal:
   ```bash
   dotnet build
