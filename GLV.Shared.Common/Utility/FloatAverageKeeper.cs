using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Common.Utility;

/// <summary>
/// Maintains a list of data and presents an arithmetic mean average of the stored data
/// </summary>
public sealed class AverageKeeper<TData> 
    where TData : struct, IFloatingPointIeee754<TData>
{
    private readonly TData[] data;
    private readonly int Size;
    private int Fill;
    private int Index;

    private bool cacheValid;
    private TData cache;

    /// <summary>
    /// Instantiates a new object of type <see cref="AverageKeeper{TData}"/>
    /// </summary>
    /// <param name="size">The amount of data to keep an average of</param>
    public AverageKeeper(int size)
    {
        if (size <= 0)
            throw new ArgumentException("size must be larger than 0", nameof(size));
        data = new TData[size];
        Size = size;
    }
    /// <summary>
    /// The calculated average based on stored data
    /// </summary>
    public TData Average
    {
        get
        {
            int fill = Fill;
            if (fill is 0)
                return TData.Zero;
            if (!cacheValid)
            {
                TData dat = TData.Zero;
                int i = 0;
                for (; i < fill - 2; i += 3)
                {
                    TData x = data[i];
                    TData y = data[i + 1];
                    TData z = data[i + 2];
                    dat += x + y + z;
                }
                for (; i < fill - 1; i += 2)
                {
                    TData x = data[i];
                    TData y = data[i];
                    dat += x + y;
                }
                while (i < fill)
                    dat += data[i++];
                cacheValid = true;
                cache = dat / TData.CreateSaturating(fill);
            }
            return cache;
        }
    }

    /// <summary>
    /// Pushes a new value into the data list, replacing the oldest value if the list is full
    /// </summary>
    /// <remarks>
    /// This method makes no attempt to check for modality. If you wish to keep outliers out, do so yourself
    /// </remarks>
    /// <param name="value">The value to push</param>
    public void Push(TData value)
    {
        cacheValid = false;
        data[Index++] = value;
        if (Index >= Size)
            Index = 0;
        if (Fill < Size) Fill++;
    }

    /// <summary>
    /// Clears all data from the list
    /// </summary>
    public void Clear()
    {
        Fill = 0;
        Index = 0;
    }
}