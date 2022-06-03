using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using FellowOakDicom;
using Jint;

namespace DcmTransform
{
    public static class Program
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable ClassNeverInstantiated.Global
        public class Options
        {
            [Value(0, HelpText = "Transform these DICOM files. When missing, this option will be read from the piped input.", Required = false)]
            public IEnumerable<string>? Files { get; set; }

            [Option('s', "script", Required = true, HelpText = "Script that transforms the provided DICOM file")]
            public string? Script { get; set; }
            
            [Option('p', "parallelism", Default = 8, HelpText = "Transform this many files in parallel")]
            public int Parallelism { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore ClassNeverInstantiated.Global

        public static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                e.Cancel = true;
            };
            
            var parserResult = Parser.Default.ParseArguments<Options>(args);

            if (parserResult is Parsed<Options> parsed)
            {
                await TransformAsync(parsed.Value, cts.Token).ConfigureAwait(false);
            }
            else if (parserResult is NotParsed<Options> notParsed)
            {
                Fail(notParsed.Errors);
            }
        }

        private static void Fail(IEnumerable<Error> errors)
        {
            Console.Error.WriteLine("Invalid arguments provided");
            foreach (var error in errors.Where(e => e.Tag != ErrorType.HelpRequestedError))
            {
                Console.Error.WriteLine(error.ToString());
            }
        }

        private static async Task TransformAsync(Options options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            IEnumerable<FileInfo> ReadFilesFromConsole()
            {
                string? file;
                while ((file = Console.ReadLine()) != null)
                {
                    if (File.Exists(file))
                        yield return new FileInfo(file);
                }
            }

            var files = options.Files != null && options.Files.Any()
                ? options.Files.Select(f => new FileInfo(f))
                : ReadFilesFromConsole();
            var script = options.Script!;
            var parallelism = options.Parallelism;

            await Task.WhenAll(
                Partitioner
                    .Create(files)
                    .GetPartitions(parallelism)
                    .AsParallel()
                    .Select(partition => TransformFilesAsync(partition, script, cancellationToken))
            ).ConfigureAwait(false);
        }

        private static async Task TransformFilesAsync(IEnumerator<FileInfo> files, string script, CancellationToken cancellationToken)
        {
            var scriptEngine = new Engine(cfg => cfg.CatchClrExceptions());
            var scriptText = File.Exists(script)
                ? await File.ReadAllTextAsync(script, cancellationToken)
                : script;
            var transformer = new DicomTransformer(scriptEngine);

            try
            {
                scriptEngine.Execute(scriptText);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync("Invalid script: " + e);
                return;
            }

            using (files)
            {
                while (files.MoveNext() && !cancellationToken.IsCancellationRequested)
                {
                    await TransformFileAsync(files.Current, transformer, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task TransformFileAsync(FileInfo file, DicomTransformer transformer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            DicomFile dicomFile;
            await using (var inputFileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
            {
                try
                {
                    dicomFile = await DicomFile.OpenAsync(inputFileStream, FileReadOption.ReadAll);
                }
                catch
                {
                    await Console.Error.WriteLineAsync("Not a DICOM file: " + file.FullName);
                    return;
                }
            }

            if (file.Name == "DICOMDIR" || dicomFile.FileMetaInfo.MediaStorageSOPClassUID == DicomUID.MediaStorageDirectoryStorage)
            {
                // Do not transform DICOM directory files, just delete them.
                File.Delete(file.FullName);
                return;
            }

            try
            {
                await transformer.TransformAsync(dicomFile.FileMetaInfo, dicomFile.Dataset, cancellationToken);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Failed to transform the provided DICOM file: {file.FullName}\n{e}");
                return;
            }

            try
            {
                await dicomFile.SaveAsync(file.FullName);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Failed to overwrite the original DICOM file: {file.FullName}\n{e}");
                return;
            }

            Console.WriteLine(file.FullName);
        }
    }
}
