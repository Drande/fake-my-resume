﻿using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;
using MakeMyResume.Data.Models;
using MakeMyResume.Services.Interfaces;
using MakeMyResume.Services.Utils;
using System.Reflection;

namespace MakeMyResume.Services
{
    public class DocumentGenerationService : IDocumentGenerationService
    {
        private readonly string _assetsPath;
        private Color greyColor = new DeviceCmyk(3, 4, 4, 4);
        private Color blueColor = new DeviceCmyk(99, 79, 0, 0);

        public DocumentGenerationService()
        {
            var assemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            _assetsPath = System.IO.Path.Combine(assemblyPath, "Assets");
        }
        public Stream GenerateResumeInPDF(Resume resume)
        {
            #region Init
            MemoryStream ms = new MemoryStream();
            PdfWriter pw = new PdfWriter(ms);
            PdfDocument pdfDocument = new PdfDocument(pw);
            PdfPage page = pdfDocument.AddNewPage();
            Document document = new Document(pdfDocument);
            document.SetMargins(60, 35, 35, 35);
            #endregion

            #region Styles

            string fontPath = System.IO.Path.Combine(_assetsPath, "Segoe UI.ttf");
            PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); 

            Style textTitleStyle = new Style();
            textTitleStyle.SetFont(font).SetBold().SetCharacterSpacing(2);
            Style paragraphTitleStyle = new Style();
            paragraphTitleStyle.SetFont(font).SetFontSize(12).SetFontColor(ColorConstants.DARK_GRAY);
            Style textDescription = new Style();
            textDescription.SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.JUSTIFIED);
            #endregion

            #region Header
            addRectangleHeader(pdfDocument, page);
            document.Add(addImage());
            addTextHeader(page, font, resume.FullName, resume.Email, resume.CurrentRole);
            #endregion

            #region Profile
            var profile = new Paragraph(new Text("PROFILE").AddStyle(textTitleStyle).AddStyle(paragraphTitleStyle)).SetMarginTop(60);
            document.Add(profile);
            document.Add(addDescription(font, resume.Description));
            #endregion

            #region Technical Skills

            var techenicalSkills = new Paragraph(new Text("TECHNICAL SKILLS").AddStyle(textTitleStyle).AddStyle(paragraphTitleStyle)).SetMarginTop(10);
            document.Add(techenicalSkills);

            Style tableTitleStyle = new Style();
            tableTitleStyle.SetFont(font).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetBorder(Border.NO_BORDER).SetBackgroundColor(greyColor);
            Style tableTextStyle = new Style();
            tableTextStyle.SetFont(font).SetFontColor(ColorConstants.BLACK).SetFontSize(9).SetBold().SetCharacterSpacing(1);

            float[] columnWidths = new float[] { 230, 120, 150 };
            Table table = new Table(columnWidths);

            Cell techologiesCell = new Cell(1, 0).AddStyle(tableTitleStyle).Add(new Paragraph(new Text("TECHNOLOGIES:").AddStyle(tableTextStyle)));
            Cell methodologiesCell = new Cell(1, 1).AddStyle(tableTitleStyle).Add(new Paragraph(new Text("METODOLOGIES:").AddStyle(tableTextStyle)));
            Cell certificationsCell = new Cell(1, 1).AddStyle(tableTitleStyle).Add(new Paragraph(new Text("CERTIFICATIONS:").AddStyle(tableTextStyle)));

            // header table
            table.AddHeaderCell(techologiesCell);
            table.AddHeaderCell(methodologiesCell);
            table.AddHeaderCell(certificationsCell);
            /// technologies table
            table.AddCell(addNestedTableWithTwoColumns(resume?.Technologies, font));
            /// methodologies table
            table.AddCell(addNestedTable(resume?.Methodologies, font));
            /// certifications table
            table.AddCell(addNestedTable(resume?.Certifications, font));
            table.SetMargin(10);
            document.Add(table);

            #endregion

            #region Work Experience

            var workExperience = new Paragraph(new Text("WORK EXPERIENCE ").AddStyle(textTitleStyle).AddStyle(paragraphTitleStyle)).SetMarginTop(10);
            document.Add(workExperience);

            resume.WorkExperience?.ForEach(we =>
            {
                document.Add(addWorkExperienceTable(textTitleStyle, tableTextStyle, textDescription, font, we));
            });

            #endregion

            #region Education

            var education = new Paragraph(new Text("EDUCATION ").AddStyle(textTitleStyle).AddStyle(paragraphTitleStyle)).SetMarginTop(10);
            document.Add(education);

            resume.Education?.ForEach(e =>
            {
                document.Add(addEducationTable(textDescription, font, e));
            });

            #endregion

            document.Close();

            #region Streams
            byte[] byteStream = ms.ToArray();

            ms = new MemoryStream();
            ms.Write(byteStream, 0, byteStream.Length);
            ms.Position = 0;
            return ms;
            #endregion
        }
        private void addRectangleHeader(PdfDocument pdfDocument, PdfPage page)
        {
            PdfCanvas canvasHeaderRectangle = new PdfCanvas(pdfDocument.GetFirstPage());
            Rectangle rectangle = new Rectangle(0, page.GetPageSize().GetTop() - 90, page.GetPageSize().GetWidth(), 90);
            canvasHeaderRectangle.Rectangle(rectangle)
                 .SetFillColor(greyColor)
                 .Fill();
        }
        private void addTextHeader(PdfPage page, PdfFont font, string resumeFullName, string resumeEmail, string resumeCurrentRole)
        {
            Rectangle rectangle = new Rectangle(35, page.GetPageSize().GetTop(), 740, 150);
            Canvas canvasTextRectangle = new Canvas(page, rectangle);
            Text fullName = new Text(resumeFullName).SetFont(font).SetBold().SetCharacterSpacing(2);
            Text email = new Text(resumeEmail).SetFont(font);
            Text currentRole = new Text(resumeCurrentRole).SetFont(font);

            canvasTextRectangle.ShowTextAligned(new Paragraph(fullName).SetFontSize(25).SetFontColor(ColorConstants.BLACK), 35, 790, TextAlignment.LEFT)
                .ShowTextAligned(new Paragraph((currentRole).SetBold().SetCharacterSpacing(1)).SetFontSize(10).SetFontColor(blueColor), 35, 775, TextAlignment.LEFT)
                .ShowTextAligned(new Paragraph(email).SetFontSize(10).SetFontColor(ColorConstants.DARK_GRAY), 35, 758, TextAlignment.LEFT)
                .Close();
        }
        private Image addImage()
        {
            string imagePath = System.IO.Path.Combine(_assetsPath, "logo_unosquare.PNG");
            ImageData logo = ImageDataFactory.Create(imagePath);

            Image image = new Image(logo).ScaleAbsolute(130, 130).SetFixedPosition(1, 440, 735);
            return image;
        }
        private Paragraph addDescription(PdfFont font, string resumeDescription)
        {
            Paragraph description = new Paragraph(resumeDescription)
                      .SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.JUSTIFIED);
            description.SetMargin(10);
            return description;
        }
        private Table addWorkExperienceTable(Style textTitleStyle, Style tableTextStyle, Style textDescription, PdfFont font, WorkExperience workExperience)
        {
            float[] workExperienceWidth = { 800f };
            Table workExperienceTable = new Table(workExperienceWidth);

            var description = workExperience.Description;

            string fullDate  = $"({Months.findMonth(workExperience.FromMonth)} {workExperience.FromYear} - {Months.findMonth(workExperience.ToMonth)} {workExperience.ToYear})";
            var fullTitle = new Paragraph(new Text(workExperience.CompanyName).AddStyle(textTitleStyle).SetFontColor(blueColor));
            fullTitle.Add(new Text($" {workExperience.ProjectName} ").AddStyle(textTitleStyle).SetFontColor(blueColor));
            fullTitle.Add(new Text(fullDate).SetFontColor(ColorConstants.BLACK).SetBold().SetCharacterSpacing(1).SetFontSize(11));


            Cell title = new Cell(1, 0).Add(fullTitle).SetBorder(Border.NO_BORDER);
            Cell twoCell = new Cell(1, 0).Add(new Paragraph(new Text(workExperience.Role).SetFont(font).SetFontColor(ColorConstants.BLACK).SetBold().SetFontSize(9).SetItalic().SetCharacterSpacing(1))).SetBorder(Border.NO_BORDER);
            Cell threeCell = new Cell(1, 0).Add(new Paragraph(new Text(description).AddStyle(textDescription))).SetBorder(Border.NO_BORDER);

            SolidLine line = new SolidLine(2f);
            line.SetColor(greyColor);
            LineSeparator ls = new LineSeparator(line);



            workExperienceTable.AddCell(title);
            workExperienceTable.AddCell(twoCell);
            workExperienceTable.AddCell(threeCell);

            workExperience.Projects.ForEach(p =>
            {
                Cell res = new Cell().Add(addProjectTable(tableTextStyle, textDescription, font, p)).SetBorder(Border.NO_BORDER);
                workExperienceTable.AddCell(res);
                Cell lineSeparator = new Cell().Add(ls).SetBorder(Border.NO_BORDER);
                workExperienceTable.AddCell(lineSeparator);
            });
            workExperienceTable.SetMargin(10);

            return workExperienceTable;
        }
        private Table addProjectTable(Style tableTextStyle, Style textDescription, PdfFont font, Project project)
        {
            float[] columns = { 800 };
            Table projectTable = new Table(columns);

            Cell projectNameTitle = new Cell(0, 0).Add(new Paragraph(new Text("Project Name:").AddStyle(tableTextStyle))).SetBorder(Border.NO_BORDER);
            projectTable.AddCell(projectNameTitle);
            Cell name = new Cell(0, 0).Add(new Paragraph(new Text(project.Name).AddStyle(textDescription))).SetBorder(Border.NO_BORDER);
            projectTable.AddCell(name);
            Cell descriptionTitle = new Cell(0, 0).Add(new Paragraph(new Text("Description:").AddStyle(tableTextStyle))).SetBorder(Border.NO_BORDER);
            projectTable.AddCell(descriptionTitle);
            Cell description = new Cell(1, 0).Add(new Paragraph(new Text(project.Description).AddStyle(textDescription))).SetBorder(Border.NO_BORDER);
            projectTable.AddCell(description);
            Cell technologiesUsedTitle = new Cell(1, 0).Add(new Paragraph(new Text("Technologies used:").AddStyle(tableTextStyle))).SetBorder(Border.NO_BORDER);
            projectTable.AddCell(technologiesUsedTitle);

            projectTable.AddCell(addNestedTableWithTwoColumns(project.TechnologiesUsed, font));
            projectTable.SetMarginTop(10);
            return projectTable;
        }
        private Table addEducationTable(Style textDescription, PdfFont font, Education education)
        {
            float[] educationWidth = { 800 };
            Table educationTable = new Table(educationWidth);

            string fullEducationTitle = $"{education.UniversityName} - {education.Major}";
            string fullSubtitle = $"{education.Degree} - {education.YearOfCompletion}";
            var pFullEducation = new Paragraph(new Text(fullEducationTitle).SetFont(font).SetBold().SetCharacterSpacing(1).SetFontSize(11)).SetFontColor(blueColor);
            var pFullSubtitle = new Paragraph(new Text(fullSubtitle).SetFont(font).SetBold().SetCharacterSpacing(1).SetFontSize(8).SetFontColor(ColorConstants.BLACK).AddStyle(textDescription));

            Cell titleEducation = new Cell(1, 0).Add(pFullEducation).SetBorder(Border.NO_BORDER);
            Cell twoCellEducation = new Cell(1, 0).Add(pFullSubtitle).SetBorder(Border.NO_BORDER);

            educationTable.AddCell(titleEducation);
            educationTable.AddCell(twoCellEducation);
            educationTable.SetMargin(10);

            return educationTable;
        }

        private Cell addNestedTableWithTwoColumns(List<string> list, PdfFont font)
        {
            float[] cellThechnologiesWidth = { 7, 90, 7, 90 };

            Table techologiesCellTable = new Table(cellThechnologiesWidth);
            for (int i = 0; i < list.Count; i++)
            {
                if (list.Count % 2 == 0)
                {
                    techologiesCellTable.AddCell(addNestedCell(true));
                    techologiesCellTable.AddCell(addNestedCell(false, list[i]));
                } 
                else
                {
                    techologiesCellTable.AddCell(addNestedCell(true));
                    techologiesCellTable.AddCell(addNestedCell(false, list[i]));
                }
            }
            Cell technologiesCell = new Cell().SetBorder(Border.NO_BORDER).Add(techologiesCellTable);
            return technologiesCell;
        }
        private Cell addNestedTable(List<string> list, PdfFont font)
        {
            float[] columns = { 7, 400 };
            Table table = new Table(columns);

            for (int i = 0; i < list.Count; i++)
            {
                table.AddCell(addNestedCell(true));
                table.AddCell(addNestedCell(false, list[i]));

            }
            Cell result = new Cell().SetBorder(Border.NO_BORDER).Add(table);
            return result;
        }
        private Cell addNestedCell(bool isBullet, string value = "\u2022")
        {
            if (isBullet)
            {
                var bullet = new Paragraph(new Text(value).SetBold().SetFontColor(blueColor));
                Cell columnBullet = new Cell().SetBorder(Border.NO_BORDER).Add(bullet);
                return columnBullet;
            } 
            else
            {
                var message = new Paragraph(new Text(value).SetFontSize(10).SetFontColor(ColorConstants.BLACK));
                Cell columnMessage = new Cell().SetBorder(Border.NO_BORDER).Add(message.SetVerticalAlignment(VerticalAlignment.BOTTOM));
                return columnMessage;
            }
        }        
        private Resume createLocalResume() 
        {
            var resume = new Resume();
            resume.Id = 1;
            resume.AccountId = "001";
            resume.FullName = "Jose Perez Lopez";
            resume.CurrentRole = ".NET Software Developer";
            resume.Email = "jose.perez@unosquare.com";
            resume.Description = "Software Developer with 11 years of proven experience on software development, primarily using .NET technologies, for Web, Cloud and Desktop platforms. Broad knowledge of C# language and JavaScript, along with SQL Server databases, as well as resource management, development, and deployment on Microsoft Azure.";
            resume.Technologies = new List<string> { "Angular v6 - v12", ".NET", "ASP.NET", ".NET Core", "SQL", "Entity Framework Core", "MONGO DB", "MYSQL", "JavaScript", "React Native" };
            resume.Methodologies = new List<string> { "Methodologies used SCRUM", "KANBAN" };
            resume.Certifications = new List<string> { "Microsoft Certified Professional (MCID 12311731)", "Aspectos básicos de Microsoft 365 MS-900", "HTML5 Javascript 325 MS-262" };
            resume.WorkExperience = new List<WorkExperience> { 
                new WorkExperience() { 
                    FromYear = 2018,
                    FromMonth = 1,
                    ToYear = 2022,
                    ToMonth = 12,
                    CompanyName = "Unosquare",
                    ProjectName = ".NET Center of Excellence",
                    Role = "Software developer",
                    Description = "Currently assigned to the .NET Center of Excellence. Here we are working on different areas of improvement in\r\nthe C# language as well as consolidating and learning new features of the .NET platform.",
                    Projects= new List<Project> { new Project()
                    {
                        Name = "Make my resume",
                        Description = "Creation of a new tool to make a resume",
                        TechnologiesUsed = new List<string> { ".NET", "ASP.NET", ".NET Core" }
                    } 
                  }
                },
                new WorkExperience() {
                    FromYear = 2016,
                    FromMonth = 9,
                    ToYear = 2018,
                    ToMonth = 5,
                    CompanyName = "Microsoft",
                    ProjectName = "Update DLL",
                    Role = "Software developer",
                    Description = "Currently assigned to the .NET Center of Excellence. Here we are working on different areas of improvement in\r\nthe C# language as well as consolidating and learning new features of the .NET platform.",
                    Projects= new List<Project> { 
                        new Project()
                        {
                            Name = "upadte dll",
                            Description = "update a DLL tool",
                            TechnologiesUsed = new List<string> { ".NET", "ASP.NET", ".NET Core", "Telerik", "Windows Form" }
                        },
                        new Project()
                        {
                            Name = "CFDI update",
                            Description = "update a CFDI tool",
                            TechnologiesUsed = new List<string> { ".NET", "ASP.NET", ".NET Core", "Telerik", "Windows Form", "Itext 7" }
                        }
                  }
                }
            };
            resume.Education = new List<Education> {
                new Education()
                {
                    Degree = "Maestria",
                    Major = "Análisis de datos",
                    UniversityName = "Instituto Politécnico Naciona",
                    YearOfCompletion = "2020"
                },
                new Education()
                {
                    Degree = "Licenciatura",
                    Major = "Software engineer",
                    UniversityName = "Universidad la Salle",
                    YearOfCompletion = "2015"
                }
            };

            return resume;
        }
    }
}
