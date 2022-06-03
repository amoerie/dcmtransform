using System;
using System.Linq;
using FellowOakDicom;

namespace DcmTransform;

public interface IJintDicom
{
    string? GetString(ushort group, ushort element);
    string[]? GetMultipleStrings(ushort group, ushort element);
    void UpdateString(ushort group, ushort element, string value);
    void DeleteTag(ushort group, ushort element);
    void DeleteGroup(ushort group);
    bool Contains(ushort group, ushort element);
    bool HasValue(ushort group, ushort element);
    int CountSequenceItems(ushort group, ushort element);
    JintDicom GetSequenceItem(ushort group, ushort element, int index);
    JintDicom[] GetSequenceItems(ushort group, ushort element);
    JintDicom CreateAndReturnSequenceItem(ushort group, ushort element);
}

public class JintDicom : IJintDicom
{
    private readonly DicomDataset _data;
    private readonly DicomFileMetaInformation? _metaData;

    public JintDicom(DicomFileMetaInformation metaData, DicomDataset data)
    {
        _data = data;
        _metaData = metaData;
    }

    public JintDicom(DicomDataset dataset)
    {
        _data = dataset;
        _metaData = null;
    }

    public string? GetString(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);

        return GetDataset(group).TryGetSingleValue(tag, out string value) ? value : null;
    }

    public string[]? GetMultipleStrings(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);

        return GetDataset(group).TryGetValues(tag, out string[] values) ? values : null;
    }

    public void UpdateString(ushort group, ushort element, string value)
    {
        var tag = DicomTag(group, element);

        GetDataset(group).AddOrUpdate(tag, value);
    }

    public void DeleteTag(ushort group, ushort element)
    {
        var tagToDelete = DicomTag(group, element);

        var ds = GetDataset(group);
        if (ds.Contains(tagToDelete))
            ds.Remove(tagToDelete);
    }

    public void DeleteGroup(ushort group)
    {
        GetDataset(group).Remove(x => x.Tag.Group == group);
    }

    public bool Contains(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);

        return GetDataset(group).Contains(tag);
    }

    public bool HasValue(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);
        var ds = GetDataset(group);

        if (!ds.Contains(tag))
            return false;

        return ds.GetDicomItem<DicomElement>(tag).Length > 0;
    }

    public int CountSequenceItems(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);
        var ds = GetDataset(group);

        if (!ds.Contains(tag))
            return 0;

        return ds.GetSequence(tag).Count();
    }

    public JintDicom GetSequenceItem(ushort group, ushort element, int index)
    {
        var tag = DicomTag(group, element);

        var ds = GetDataset(group).GetSequence(tag).ElementAt(index);
        return new JintDicom(ds);
    }

    public JintDicom[] GetSequenceItems(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);

        return GetDataset(group).GetSequence(tag).Select(ds => new JintDicom(ds)).ToArray();
    }

    public JintDicom CreateAndReturnSequenceItem(ushort group, ushort element)
    {
        var tag = DicomTag(group, element);
        var ds = GetDataset(group);

        if (!ds.Contains(tag))
        {
            ds.Add(new DicomSequence(tag));
        }

        var sqItem = new DicomDataset();
        var sq = ds.GetSequence(tag);
        sq.Items.Add(sqItem);

        return new JintDicom(sqItem);
    }


    private DicomTag DicomTag(ushort group, ushort element)
    {
        var tag = new DicomTag(group, element);

        if (!tag.IsPrivate)
            return tag;

        for (ushort privateCreatorElement = 0x0010; privateCreatorElement <= 0x00ff; privateCreatorElement++)
        {
            if (privateCreatorElement == element)
                continue;

            var privateCreatorTag = new DicomTag(group, privateCreatorElement);
            if (GetDataset(group).TryGetSingleValue(privateCreatorTag, out string privateCreatorValue))
            {
                return new DicomTag(group, element, privateCreatorValue);
            }
        }

        return tag;
    }

    private DicomDataset GetDataset(ushort group)
    {
        if (group <= 0x0002)
        {
            if (_metaData == null)
                throw new InvalidOperationException("Cannot read data from meta data from a nested DICOM data set");
            return _metaData;
        }

        return _data;
    }
}
