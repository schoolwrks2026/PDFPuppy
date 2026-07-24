Role:
You are a Top 1% Software Architect, System Analyst, UX Designer, and Desktop Application Engineer.

Objective:
Design and build a production-ready, lightweight, high-performance Windows Desktop PDF Utility application that is simple, modern, responsive, and easy to use.

Application Name:
PDF Utility

Primary Goal:
Provide a one-click desktop utility to convert various file types into PDF and merge multiple files into a single PDF while maintaining maximum document quality.

Design Principles:
- Desktop-first
- Clean and minimal UI
- Fast startup
- Drag-and-drop support
- Offline operation
- No internet required
- Privacy-first (all processing performed locally)
- Low memory usage
- High performance
- Beginner-friendly
- Responsive layout
- Dark and Light themes

--------------------------------------------------
MODULE 1 – Dashboard
--------------------------------------------------

Provide a simple home screen with two primary actions:

• Convert to PDF
• Merge PDFs

Include:
- Recent files
- Last output folder
- Settings shortcut
- Drag & Drop area

--------------------------------------------------
MODULE 2 – Convert Files to PDF
--------------------------------------------------

Supported Input Formats:

Documents
- DOC
- DOCX
- RTF
- TXT
- ODT

Spreadsheets
- XLS
- XLSX
- ODS
- CSV

Presentations
- PPT
- PPTX
- ODP

Images
- JPG
- JPEG
- PNG
- BMP
- GIF
- TIFF
- WEBP

Output

Single PDF

Features

- Drag & Drop
- Browse files
- Multi-file selection
- Preserve formatting
- Maintain image quality
- Auto page sizing
- Auto orientation
- Output folder selection
- Open output after completion
- Progress indicator

Validation

- Unsupported file type
- Corrupted file
- Password protected Office file
- Missing fonts
- Empty document
- Duplicate file selection
- Insufficient disk space
- Invalid output folder
- Existing output filename conflict
- Conversion failure logging

--------------------------------------------------
MODULE 3 – Merge PDF
--------------------------------------------------

Allow users to merge multiple PDF files into one.

Features

- Drag & Drop
- Browse PDFs
- Reorder by drag-and-drop
- Remove selected file
- Remove all
- Preview file list
- Display page count
- Display file size
- Output filename
- Output folder
- Merge progress
- Open merged PDF after completion

Validation

- Empty PDF
- Corrupted PDF
- Password protected PDF
- Duplicate PDF
- Large PDFs
- Hundreds of PDFs
- Missing source file
- Merge interruption
- Existing destination file

--------------------------------------------------
MODULE 4 – Settings
--------------------------------------------------

Options

Default Output Folder

Theme
- Light
- Dark
- System

Open PDF after completion

Remember last folder

Overwrite existing files
(Default OFF)

Language ready for localization

--------------------------------------------------
MODULE 5 – History
--------------------------------------------------

Maintain local history of

- Converted files
- Merged PDFs
- Date
- Time
- Output path
- Status

Allow

- Open output
- Open folder
- Clear history

--------------------------------------------------
MODULE 6 – Error Handling
--------------------------------------------------

Show friendly messages.

Examples

Unsupported format

Conversion failed

Merge failed

Output path unavailable

Permission denied

Disk full

File already exists

Provide actionable recovery suggestions.

--------------------------------------------------
NON-FUNCTIONAL REQUIREMENTS
--------------------------------------------------

Offline only

No cloud dependency

No telemetry

No advertisements

No login

No user account

Support files less than 500 MB

Handle thousands of pages

Process large batches efficiently

Prevent UI freezing using background tasks

Show progress bar

Allow cancellation

Recover gracefully from crashes

Automatic resource cleanup

Comprehensive logging

High DPI support

Keyboard shortcuts

Accessible UI

--------------------------------------------------
UI REQUIREMENTS
--------------------------------------------------

Modern Windows desktop interface

Rounded controls

Minimalist design

Drag-and-drop support

Large action buttons

Status bar

Progress bar

File list with icons

Responsive resizing

Dark mode

Light mode

--------------------------------------------------
EDGE CASES
--------------------------------------------------

Missing source file after selection

Duplicate filenames

Duplicate PDFs

Long file paths

Unicode filenames

Special characters

Very large images

Very large Office files

Low disk space

Read-only folders

Permission errors

Interrupted processing

Application closed during processing

System sleep during processing

Corrupted PDFs

Corrupted Office documents

Corrupted images

Unsupported encoding

Network drive unavailable

Output filename conflicts

--------------------------------------------------
TECHNOLOGY STACK
--------------------------------------------------

Platform:
Windows Desktop

Framework:
.NET 9

UI:
WPF

Language:
C#

Architecture:
MVVM

Dependency Injection:
Microsoft.Extensions.DependencyInjection

Logging:
Serilog

PDF Library:
PDFsharp or iText7

Office Conversion:
Microsoft Office Interop (if available) with fallback libraries

Image Processing:
ImageSharp

Packaging:
MSIX

--------------------------------------------------
OUTPUT EXPECTATION
--------------------------------------------------

Generate a complete production-ready solution including:

- Folder structure
- Project architecture
- MVVM implementation
- UI screens
- Models
- ViewModels
- Services
- Interfaces
- Dependency Injection
- Exception handling
- Logging
- File conversion workflow
- PDF merge workflow
- Validation
- Progress reporting
- Unit tests
- Build instructions
- Deployment guide

Write clean, modular, scalable, maintainable code following SOLID principles, asynchronous programming best practices, and proper separation of concerns. The application should be immediately extensible for future features such as Split PDF, Compress PDF, OCR, Watermark, Password Protection, and Digital Signature without requiring architectural redesign.
