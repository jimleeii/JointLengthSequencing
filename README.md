# Joint Length Sequencing

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Joint Length Sequencing** is a .NET 8.0 project that utilizes a minimal API structure. This project is designed to process and analyze joint length sequencing data.

## Features

*   **Minimal API:** Built using the minimal API approach in ASP.NET Core for a streamlined and efficient development experience.
*   **API Versioning:** Implements API versioning using `Microsoft.AspNetCore.Mvc.Versioning` to manage changes and maintain backward compatibility.
*   **OpenAPI/Swagger Support:** Includes `Swashbuckle.AspNetCore` for generating OpenAPI/Swagger documentation, making it easy to explore and test the API.
*   **JSON Serialization:** Leverages `System.Text.Json` for efficient JSON serialization and deserialization.
* **Target .NET 8.0:** Built for .NET 8.0, the latest LTS version of .NET.

## Dependencies

This project relies on the following NuGet packages:

*   **EndpointDefinition (1.0.3):** [Add a description of what this dependency does if you know it].
*   **Microsoft.AspNetCore.Mvc.Versioning (5.1.0):** For API versioning.
*   **Swashbuckle.AspNetCore (6.5.0):** For generating OpenAPI/Swagger documentation.
*   **System.Text.Json (9.0.1):** For JSON serialization and deserialization.

## Getting Started

### Prerequisites

*   .NET 8.0 SDK

### Building and Running

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd JointLengthSequencing
    ```
    * Replace `<repository-url>` with the actual repository url.
2.  **Build the project:**
    ```bash
    dotnet build -c Release
    ```
3.  **Run the project:**
    ```bash
    dotnet run --project src/JointLengthSequencing/JointLengthSequencing.csproj
    ```
    * If you have multiple projects, you need to specify the project to run.

### Packaging as a NuGet Package

1.  **Build the project in Release mode:**
    ```bash
    dotnet build -c Release
    ```
2.  **Package the project:**
    ```bash
    dotnet pack src/JointLengthSequencing.nuspec -o nupkg
    ```
    This will create a `.nupkg` file in the `nupkg` directory.
3. **Push the package:**
    ```bash
    dotnet nuget push nupkg/JointLengthSequencing.1.0.0.nupkg --source <your-nuget-feed> --api-key <your-api-key>
    ```
    * Replace `<your-nuget-feed>` and `<your-api-key>` with your actual values.

## License

This project is licensed under the MIT License.
