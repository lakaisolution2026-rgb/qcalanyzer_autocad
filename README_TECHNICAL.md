# QCValidator: AutoCAD Quality Control Analyzer

A robust .NET-based tool designed to automate Quality Control (QC) processes for AutoCAD drawings. It validates drawings against predefined templates to ensure consistency, compliance, and accuracy.

## 📖 User Story

**Persona**: *John*, a Senior Draftsman at an engineering firm.

**The Problem**: John manages hundreds of AutoCAD drawings daily. Manually checking if every layer has the correct color, if text styles are compliant, or if entities were accidentally left on "Layer 0" is tedious and prone to human error. Mistakes in these drawings can lead to costly delays during the construction or manufacturing phase.

**The Solution**: John uses the **QCValidator**. He runs the tool against his DWG files, and within seconds, he receives a detailed JSON report highlighting every discrepancy. Now, John can focus on design while the validator handles the repetitive compliance checks.

---

## 🏗️ Architecture (Clean Architecture)

The project is structured into four distinct layers, following Clean Architecture principles to ensure maintainability and testability:

### 1. QCValidator.Domain
- **Responsibility**: Contains the core business entities and logic that are independent of any external frameworks.
- **Key Models**: `DrawingEntity`, `Layer`, `QCError`, `QCReport`, `TextStyle`.

### 2. QCValidator.Application
- **Responsibility**: Houses the orchestration logic and business rules.
- **Components**:
    - **Interfaces**: Defines contracts for data providers and report generators.
    - **QCValidationService**: The "brain" of the application. It fetches data via interfaces, runs comparisons, and triggers report generation.

### 3. QCValidator.Infrastructure
- **Responsibility**: Implements the interfaces defined in the Application layer.
- **Key Providers**:
    - **AutoCadDrawingProvider**: Uses **ACadSharp** to read and parse real `.dwg` files.
    - **ExcelTemplateProvider**: (Mocked) Represents the source of truth for "correct" drawing standards.
    - **JsonReportGenerator**: Handles the serialization of validation results into a machine-readable JSON format.

### 4. QCValidator.Console
- **Responsibility**: The entry point of the application.
- **Function**: Handles CLI arguments, wires up the dependencies (Dependency Injection), and executes the validation service.

---


## 🛠️ Technology Stack

- **Runtime**: .NET (C#)
- **DWG Parsing**: [ACadSharp](https://github.com/skyruby/ACadSharp) - A powerful library for reading and writing AutoCAD files without requiring AutoCAD installed.
- **Reporting**: JSON (using `System.Text.Json` or similar)
- **Project Type**: Console Application (CLI)

---

## 🚀 Core Workflow

1. **Initialization**: The Console App starts and accepts a `.dwg` file path.
2. **Data Extraction**: The `AutoCadDrawingProvider` opens the drawing using ACadSharp and extracts Layers, Text Styles, and Entities.
3. **Template Loading**: The `ExcelTemplateProvider` provides the "Standard" (e.g., "Layer 'RRU_L' must be Color 7").
4. **Validation Logic**:
    - **Layer Check**: Compares drawing layers against template requirements (Missing layers or wrong colors).
    - **Text Style Check**: Ensures only approved fonts (Arial, etc.) are used.
    - **Layer 0 Check**: Flags any entity found on the restricted "Layer 0".
5. **Report Generation**: Results are aggregated into a `QCReport` and saved as `qc_report.json`.

---

## 🚦 How to Run

1. Ensure you have the .NET SDK installed.
2. Navigate to the root folder.
3. Run the following command:
   ```bash
   dotnet run --project src/QCValidator.Console/QCValidator.Console.csproj <path-to-your-drawing.dwg>
   ```
4. Check the generated `qc_report.json` for validation results.

to runt he project :
dotnet run --project "src/QCValidator.WebUI/QCValidator.WebUI.csproj"
1qdcqb;jv;]