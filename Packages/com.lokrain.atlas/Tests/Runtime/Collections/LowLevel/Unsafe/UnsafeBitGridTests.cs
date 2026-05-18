#nullable enable

using System;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.Atlas.Collections.LowLevel.Unsafe.Tests
{
    public sealed unsafe class UnsafeBitGridTests
    {
        [Test]
        public void Constructor_WithNegativeBitLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UnsafeBitGrid(-1, Allocator.Persistent));
        }

        [Test]
        public void Constructor_WithAllocatorNone_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => _ = new UnsafeBitGrid(1, Allocator.None));
        }

        [Test]
        public void Constructor_WithZeroBitLength_CreatesEmptyGrid()
        {
            UnsafeBitGrid grid = CreateGrid(0);

            try
            {
                Assert.That(grid.IsCreated, Is.True);
                Assert.That(grid.BitLength, Is.EqualTo(0));
                Assert.That(grid.WordLength, Is.EqualTo(0));
                Assert.That(grid.IsEmpty, Is.True);
                Assert.That(grid.ContainsBit(0), Is.False);
                Assert.That(grid.ContainsWord(0), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Constructor_WithPositiveBitLength_CreatesClearedGrid()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.That(grid.IsCreated, Is.True);
                Assert.That(grid.BitLength, Is.EqualTo(65));
                Assert.That(grid.WordLength, Is.EqualTo(2));
                Assert.That(grid.IsEmpty, Is.False);

                for (int bitIndex = 0; bitIndex < grid.BitLength; bitIndex++)
                {
                    Assert.That(grid.IsSet(bitIndex), Is.False, $"Bit {bitIndex} should be clear.");
                }
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Default_IsCreated_ReturnsFalse()
        {
            UnsafeBitGrid grid = default;

            Assert.That(grid.IsCreated, Is.False);
        }

        [Test]
        public void BitLength_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => _ = grid.BitLength);
        }

        [Test]
        public void WordLength_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => _ = grid.WordLength);
        }

        [Test]
        public void IsEmpty_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => _ = grid.IsEmpty);
        }

        [Test]
        public void ContainsBit_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.ContainsBit(0));
        }

        [Test]
        public void ContainsWord_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.ContainsWord(0));
        }

        [Test]
        public void ClearAll_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.ClearAll());
        }

        [Test]
        public void SetAll_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.SetAll());
        }

        [Test]
        public void IsSet_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.IsSet(0));
        }

        [Test]
        public void Set_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.Set(0));
        }

        [Test]
        public void Clear_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.Clear(0));
        }

        [Test]
        public void Write_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.Write(0, true));
        }

        [Test]
        public void ReadWord_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.ReadWord(0));
        }

        [Test]
        public void WriteWord_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.WriteWord(0, 1UL));
        }

        [Test]
        public void GetWordRange_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.GetWordRange(0, 0));
        }

        [Test]
        public void GetWordPartition_WhenDefault_Throws()
        {
            UnsafeBitGrid grid = default;

            Assert.Throws<InvalidOperationException>(() => grid.GetWordPartition(0, 1));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(63, 1)]
        [TestCase(64, 1)]
        [TestCase(65, 2)]
        [TestCase(127, 2)]
        [TestCase(128, 2)]
        [TestCase(129, 3)]
        public void Constructor_ComputesExpectedWordLength(int bitLength, int expectedWordLength)
        {
            UnsafeBitGrid grid = CreateGrid(bitLength);

            try
            {
                Assert.That(grid.BitLength, Is.EqualTo(bitLength));
                Assert.That(grid.WordLength, Is.EqualTo(expectedWordLength));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ContainsBit_ReturnsWhetherBitIndexIsValid()
        {
            UnsafeBitGrid grid = CreateGrid(10);

            try
            {
                Assert.That(grid.ContainsBit(-1), Is.False);
                Assert.That(grid.ContainsBit(0), Is.True);
                Assert.That(grid.ContainsBit(9), Is.True);
                Assert.That(grid.ContainsBit(10), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ContainsWord_ReturnsWhetherWordIndexIsValid()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.That(grid.ContainsWord(-1), Is.False);
                Assert.That(grid.ContainsWord(0), Is.True);
                Assert.That(grid.ContainsWord(1), Is.True);
                Assert.That(grid.ContainsWord(2), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void IsSet_WithNegativeBitIndex_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.IsSet(-1));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void IsSet_WithBitIndexEqualToBitLength_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(1);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.IsSet(1));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Set_WithInvalidBitIndex_ThrowsAndDoesNotMutate()
        {
            UnsafeBitGrid grid = CreateGrid(2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.Set(2));

                Assert.That(grid.IsSet(0), Is.False);
                Assert.That(grid.IsSet(1), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Clear_WithInvalidBitIndex_ThrowsAndDoesNotMutate()
        {
            UnsafeBitGrid grid = CreateGrid(2);

            try
            {
                grid.SetAll();

                Assert.Throws<ArgumentOutOfRangeException>(() => grid.Clear(2));

                Assert.That(grid.IsSet(0), Is.True);
                Assert.That(grid.IsSet(1), Is.True);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Write_WithInvalidBitIndex_ThrowsAndDoesNotMutate()
        {
            UnsafeBitGrid grid = CreateGrid(2);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.Write(2, true));

                Assert.That(grid.IsSet(0), Is.False);
                Assert.That(grid.IsSet(1), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SetClearAndWrite_UpdateSingleBits()
        {
            UnsafeBitGrid grid = CreateGrid(70);

            try
            {
                grid.Set(0);
                grid.Set(64);
                grid.Write(69, true);

                Assert.That(grid.IsSet(0), Is.True);
                Assert.That(grid.IsSet(1), Is.False);
                Assert.That(grid.IsSet(64), Is.True);
                Assert.That(grid.IsSet(69), Is.True);

                grid.Clear(64);
                grid.Write(69, false);

                Assert.That(grid.IsSet(0), Is.True);
                Assert.That(grid.IsSet(64), Is.False);
                Assert.That(grid.IsSet(69), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ClearAll_ClearsAllBits()
        {
            UnsafeBitGrid grid = CreateGrid(70);

            try
            {
                grid.SetAll();

                grid.ClearAll();

                for (int bitIndex = 0; bitIndex < grid.BitLength; bitIndex++)
                {
                    Assert.That(grid.IsSet(bitIndex), Is.False, $"Bit {bitIndex} should be clear.");
                }
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ClearAll_WithZeroBitLength_DoesNotThrow()
        {
            UnsafeBitGrid grid = CreateGrid(0);

            try
            {
                Assert.DoesNotThrow(() => grid.ClearAll());
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SetAll_WithZeroBitLength_DoesNotThrow()
        {
            UnsafeBitGrid grid = CreateGrid(0);

            try
            {
                Assert.DoesNotThrow(() => grid.SetAll());
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SetAll_SetsOnlyValidBitsAndMasksUnusedHighBits()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                grid.SetAll();

                for (int bitIndex = 0; bitIndex < grid.BitLength; bitIndex++)
                {
                    Assert.That(grid.IsSet(bitIndex), Is.True, $"Bit {bitIndex} should be set.");
                }

                Assert.That(grid.ReadWord(0), Is.EqualTo(ulong.MaxValue));
                Assert.That(grid.ReadWord(1), Is.EqualTo(1UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ReadWord_WithInvalidWordIndex_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.ReadWord(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.ReadWord(2));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WriteWord_WithInvalidWordIndex_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.WriteWord(-1, 0UL));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.WriteWord(2, 0UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WriteWord_WritesWholeWord()
        {
            UnsafeBitGrid grid = CreateGrid(128);

            try
            {
                grid.WriteWord(0, 0xAAAAAAAAAAAAAAAAUL);
                grid.WriteWord(1, 0x5555555555555555UL);

                Assert.That(grid.ReadWord(0), Is.EqualTo(0xAAAAAAAAAAAAAAAAUL));
                Assert.That(grid.ReadWord(1), Is.EqualTo(0x5555555555555555UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WriteWord_OnLastPartialWord_MasksUnusedHighBits()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                grid.WriteWord(1, ulong.MaxValue);

                Assert.That(grid.ReadWord(1), Is.EqualTo(1UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ClearWordRange_ClearsOnlySpecifiedWords()
        {
            UnsafeBitGrid grid = CreateGrid(192);

            try
            {
                grid.SetAll();

                grid.ClearWordRange(1, 2);

                Assert.That(grid.ReadWord(0), Is.EqualTo(ulong.MaxValue));
                Assert.That(grid.ReadWord(1), Is.EqualTo(0UL));
                Assert.That(grid.ReadWord(2), Is.EqualTo(ulong.MaxValue));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SetWordRange_SetsOnlySpecifiedWords()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                grid.SetWordRange(1, 3);

                Assert.That(grid.ReadWord(0), Is.EqualTo(0UL));
                Assert.That(grid.ReadWord(1), Is.EqualTo(ulong.MaxValue));
                Assert.That(grid.ReadWord(2), Is.EqualTo(3UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void ClearWordRange_WithInvalidRange_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.ClearWordRange(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.ClearWordRange(1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.ClearWordRange(0, 3));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SetWordRange_WithInvalidRange_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetWordRange(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetWordRange(1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetWordRange(0, 3));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordRange_WithInvalidRange_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordRange(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordRange(1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordRange(0, 3));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordRange_WithValidRange_ReturnsRange()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.That(range.IsCreated, Is.True);
                Assert.That(range.BitLength, Is.EqualTo(130));
                Assert.That(range.WordLength, Is.EqualTo(3));
                Assert.That(range.WordStart, Is.EqualTo(1));
                Assert.That(range.WordEnd, Is.EqualTo(2));
                Assert.That(range.WordCount, Is.EqualTo(1));
                Assert.That(range.IsEmpty, Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordRange_WithEmptyRange_ReturnsCreatedEmptyRange()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 1);

                Assert.That(range.IsCreated, Is.True);
                Assert.That(range.WordStart, Is.EqualTo(1));
                Assert.That(range.WordEnd, Is.EqualTo(1));
                Assert.That(range.WordCount, Is.EqualTo(0));
                Assert.That(range.IsEmpty, Is.True);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_Default_IsCreatedReturnsFalse()
        {
            UnsafeBitGridWordRange range = default;

            Assert.That(range.IsCreated, Is.False);
        }

        [Test]
        public void WordRange_DefaultPropertiesThrow()
        {
            UnsafeBitGridWordRange range = default;

            Assert.Throws<InvalidOperationException>(() => _ = range.BitLength);
            Assert.Throws<InvalidOperationException>(() => _ = range.WordLength);
            Assert.Throws<InvalidOperationException>(() => _ = range.WordStart);
            Assert.Throws<InvalidOperationException>(() => _ = range.WordEnd);
            Assert.Throws<InvalidOperationException>(() => _ = range.WordCount);
            Assert.Throws<InvalidOperationException>(() => _ = range.IsEmpty);
        }

        [Test]
        public void WordRange_DefaultMethodsThrow()
        {
            UnsafeBitGridWordRange range = default;

            Assert.Throws<InvalidOperationException>(() => range.ContainsWord(0));
            Assert.Throws<InvalidOperationException>(() => range.ContainsBit(0));
            Assert.Throws<InvalidOperationException>(() => range.IsSet(0));
            Assert.Throws<InvalidOperationException>(() => range.Set(0));
            Assert.Throws<InvalidOperationException>(() => range.Clear(0));
            Assert.Throws<InvalidOperationException>(() => range.Write(0, true));
            Assert.Throws<InvalidOperationException>(() => range.ReadWord(0));
            Assert.Throws<InvalidOperationException>(() => range.WriteWord(0, 1UL));
            Assert.Throws<InvalidOperationException>(() => range.ClearWords());
            Assert.Throws<InvalidOperationException>(() => range.SetWords());
        }

        [Test]
        public void WordRange_ContainsWord_ReturnsWhetherAbsoluteWordIsOwned()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.That(range.ContainsWord(0), Is.False);
                Assert.That(range.ContainsWord(1), Is.True);
                Assert.That(range.ContainsWord(2), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_ContainsBit_ReturnsWhetherBitWordIsOwned()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.That(range.ContainsBit(-1), Is.False);
                Assert.That(range.ContainsBit(0), Is.False);
                Assert.That(range.ContainsBit(63), Is.False);
                Assert.That(range.ContainsBit(64), Is.True);
                Assert.That(range.ContainsBit(127), Is.True);
                Assert.That(range.ContainsBit(128), Is.False);
                Assert.That(range.ContainsBit(130), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_SetClearAndWrite_UpdateOwnedBits()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                range.Set(64);
                range.Set(127);
                range.Write(65, true);

                Assert.That(range.IsSet(64), Is.True);
                Assert.That(range.IsSet(65), Is.True);
                Assert.That(range.IsSet(127), Is.True);

                Assert.That(grid.IsSet(64), Is.True);
                Assert.That(grid.IsSet(65), Is.True);
                Assert.That(grid.IsSet(127), Is.True);

                range.Clear(65);
                range.Write(127, false);

                Assert.That(grid.IsSet(64), Is.True);
                Assert.That(grid.IsSet(65), Is.False);
                Assert.That(grid.IsSet(127), Is.False);
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_Set_WithBitOutsideSourceRange_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.Throws<ArgumentOutOfRangeException>(() => range.Set(130));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_Set_WithBitOutsideOwnedWordRange_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.Throws<InvalidOperationException>(() => range.Set(0));
                Assert.Throws<InvalidOperationException>(() => range.Set(128));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_ReadAndWriteWord_UseAbsoluteWordIndex()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 3);

                range.WriteWord(1, 0xAAAAAAAAAAAAAAAAUL);
                range.WriteWord(2, ulong.MaxValue);

                Assert.That(range.ReadWord(1), Is.EqualTo(0xAAAAAAAAAAAAAAAAUL));
                Assert.That(range.ReadWord(2), Is.EqualTo(3UL));

                Assert.That(grid.ReadWord(1), Is.EqualTo(0xAAAAAAAAAAAAAAAAUL));
                Assert.That(grid.ReadWord(2), Is.EqualTo(3UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_ReadWord_WithUnownedWord_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.Throws<InvalidOperationException>(() => range.ReadWord(0));
                Assert.Throws<InvalidOperationException>(() => range.ReadWord(2));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_WriteWord_WithUnownedWord_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                Assert.Throws<InvalidOperationException>(() => range.WriteWord(0, 1UL));
                Assert.Throws<InvalidOperationException>(() => range.WriteWord(2, 1UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_ClearWords_ClearsOnlyOwnedWords()
        {
            UnsafeBitGrid grid = CreateGrid(192);

            try
            {
                grid.SetAll();

                UnsafeBitGridWordRange range = grid.GetWordRange(1, 2);

                range.ClearWords();

                Assert.That(grid.ReadWord(0), Is.EqualTo(ulong.MaxValue));
                Assert.That(grid.ReadWord(1), Is.EqualTo(0UL));
                Assert.That(grid.ReadWord(2), Is.EqualTo(ulong.MaxValue));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void WordRange_SetWords_SetsOnlyOwnedWordsAndMasksLastWord()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                UnsafeBitGridWordRange range = grid.GetWordRange(1, 3);

                range.SetWords();

                Assert.That(grid.ReadWord(0), Is.EqualTo(0UL));
                Assert.That(grid.ReadWord(1), Is.EqualTo(ulong.MaxValue));
                Assert.That(grid.ReadWord(2), Is.EqualTo(3UL));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordPartition_WithInvalidPartitionCount_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordPartition(0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordPartition(0, -1));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordPartition_WithInvalidPartitionIndex_Throws()
        {
            UnsafeBitGrid grid = CreateGrid(130);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordPartition(-1, 2));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetWordPartition(2, 2));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void GetWordPartition_PartitionsCoverWordsWithoutOverlap()
        {
            UnsafeBitGrid grid = CreateGrid(320);

            try
            {
                UnsafeBitGridWordRange partition0 = grid.GetWordPartition(0, 3);
                UnsafeBitGridWordRange partition1 = grid.GetWordPartition(1, 3);
                UnsafeBitGridWordRange partition2 = grid.GetWordPartition(2, 3);

                Assert.That(partition0.WordStart, Is.EqualTo(0));
                Assert.That(partition0.WordEnd, Is.EqualTo(1));

                Assert.That(partition1.WordStart, Is.EqualTo(1));
                Assert.That(partition1.WordEnd, Is.EqualTo(3));

                Assert.That(partition2.WordStart, Is.EqualTo(3));
                Assert.That(partition2.WordEnd, Is.EqualTo(5));

                Assert.That(partition0.WordEnd, Is.EqualTo(partition1.WordStart));
                Assert.That(partition1.WordEnd, Is.EqualTo(partition2.WordStart));
                Assert.That(partition2.WordEnd, Is.EqualTo(grid.WordLength));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void Dispose_WhenCalledTwiceOnSameValue_DoesNotThrow()
        {
            UnsafeBitGrid grid = CreateGrid(65);

            Assert.DoesNotThrow(() => grid.Dispose());
            Assert.DoesNotThrow(() => grid.Dispose());

            Assert.That(grid.IsCreated, Is.False);
        }

        private static UnsafeBitGrid CreateGrid(int bitLength)
        {
            return new UnsafeBitGrid(bitLength, Allocator.Persistent);
        }
    }
}