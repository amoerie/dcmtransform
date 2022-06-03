using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FluentAssertions;
using Xunit;

namespace DcmTransform.Tests;

public class TestsForDcmTransform
{
    [Fact]
    public async Task ShouldTransformASampleDicomFile()
    {
        // Arrange
        var sampleDicomFile = new FileInfo("./SampleDicomFile.DCM");
        var sampleTransformScript = new FileInfo("./SampleTransformScript.js");

        // Act
        await Program.Main(new string[] {
            sampleDicomFile.FullName, 
            "--script",
            sampleTransformScript.FullName
        });

        // Assert
        var transformedDicomFile = await DicomFile.OpenAsync(sampleDicomFile.FullName);
        var accessionNumber = transformedDicomFile.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
        accessionNumber.Should().Be("TRANSFORMED");
    }
}
