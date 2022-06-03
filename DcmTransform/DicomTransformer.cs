using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Jint;

namespace DcmTransform;

public class DicomTransformer
{
    private readonly Engine _scriptEngine;

    public DicomTransformer(Engine scriptEngine)
    {
        _scriptEngine = scriptEngine ?? throw new ArgumentNullException(nameof(scriptEngine));
    }

    public Task TransformAsync(DicomFileMetaInformation metaData, DicomDataset data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jintDicom = new JintDicom(metaData, data);

        _scriptEngine.Invoke("main", jintDicom);

        return Task.CompletedTask;
    }
}
