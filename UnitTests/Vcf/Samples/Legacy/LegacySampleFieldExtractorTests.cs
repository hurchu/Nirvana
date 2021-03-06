﻿using System.Linq;
using Vcf.Sample;
using Xunit;
using static UnitTests.Vcf.Samples.TestUtilities;

namespace UnitTests.Vcf.Samples.Legacy
{
    public sealed class LegacySampleFieldExtractorTests
    {

        [Fact]
        public void FormatIndicesTest()
        {
            const string formatColumn = "AU:GU:TAR:FT:GQ:DP:VF:CU:TU:TIR:GT:GQX:BOB:DPI:NV:NR:CHC:DST:PCH:DCS:DID:PLG:PCN:MAD:SCH:AQ:LQ";
            var formatIndicies = new FormatIndices();
            formatIndicies.Set(formatColumn);

            Assert.Equal(0, formatIndicies.AU);
            Assert.Equal(7, formatIndicies.CU);
            Assert.Equal(1, formatIndicies.GU);
            Assert.Equal(8, formatIndicies.TU);
            Assert.Equal(2, formatIndicies.TAR);
            Assert.Equal(9, formatIndicies.TIR);
            Assert.Equal(3, formatIndicies.FT);
            Assert.Equal(10, formatIndicies.GT);
            Assert.Equal(4, formatIndicies.GQ);
            Assert.Equal(11, formatIndicies.GQX);
            Assert.Equal(5, formatIndicies.DP);
            Assert.Equal(6, formatIndicies.VF);
            Assert.Equal(13, formatIndicies.DPI);
            Assert.Equal(17, formatIndicies.DST);
            Assert.Equal(25, formatIndicies.AQ);
            Assert.Equal(26, formatIndicies.LQ);

            formatIndicies.Set(null);
            Assert.Null(formatIndicies.TIR);
            Assert.Null(formatIndicies.AU);


            formatIndicies.Set("TEMP:DPI:BOB");
            Assert.Equal(1, formatIndicies.DPI);
            Assert.Null(formatIndicies.AU);
        }

        [Theory]
        [InlineData("GT:TIR:TAR", "1/1:18,19:37,38", new[] { 37, 18 })]
        [InlineData("GT:AU:CU:GU:TU:AD", "1/1:10,11:20,21:30,31:40,41:11,13", new[] { 20, 40 })]
        [InlineData("GT:AD", "1/1:11,13", new[] { 11, 13 })]
        [InlineData("GT:AU:CU:GU:TU:AD", "1/1:.:20,21:30,31:40,41:11,13", new[] { 11, 13 })]
        [InlineData("GT:AU:CU:GU:TU:AD", "1/1:.", null)]//null when all fields are dropped after GT
        [InlineData("GT:AU:CU:GU:TU:AD", "1/1", null)]//null when all fields are dropped after GT
        [InlineData("AD", ".", null)]
        [InlineData("AD", "", null)]
        public void AlleleDepths(string formatCol, string sampleCol, int[] expectedAlleleDepths)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedAlleleDepths = sample?.AlleleDepths;
            Assert.Equal(expectedAlleleDepths, observedAlleleDepths);
        }

        [Theory]
        [InlineData("GT:TIR:TAR", "1/1:18,19:37,38", null)]
        [InlineData("GT:TIR:TAR:AD", "1/1:.:37,38:11,13,17", new[] { 11, 13, 17 })]
        [InlineData("GT:TIR:TAR:AD", "1/1:.:37,38", null)]//null when no values for AD for multi-allelic site
        public void AlleleDepthsMultiAllelic(string formatCol, string sampleCol, int[] expectedAlleleDepths)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT,A\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(2), null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedAlleleDepths = sample?.AlleleDepths;
            Assert.Equal(expectedAlleleDepths, observedAlleleDepths);
        }

        [Theory]
        [InlineData("1/1:208:47:70:3:F", true)]
        [InlineData("1/1:208:47:70:3:.", false)]
        [InlineData(".", false)]
        [InlineData("", false)]
        public void FailedFilter(string sampleCol, bool? expectedFailedFilter)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\tGT:GQ:GQX:DP:DPF:FT\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(2), null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedFailedFilter = sample?.FailedFilter;
            Assert.Equal(expectedFailedFilter, observedFailedFilter);
        }

        [Theory]
        [InlineData("1/1:208:47:70:3:0,70", "1/1")]
        [InlineData(".:208:47:70:3:0,70", null)]
        [InlineData(".", null)]
        [InlineData("", null)]
        [InlineData("./.", "./.")]
        public void Genotype(string sampleCol, string expectedGenotype)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\tGT:GQ:GQX:DP:DPF:AD\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1),null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedGenotype = sample?.Genotype;
            Assert.Equal(expectedGenotype, observedGenotype);
        }

        [Theory]
        [InlineData("GT:GQ:GQX:DP:DPF:AD", "1/1:208:47:70:3:0,70", 47)]
        [InlineData("GT:GQ:DP:DPF:AD", "1/1:208:70:3:0,70", 208)]
        [InlineData("GT:GQ:DP:DPF:AD", "1/1:.:70:3:0,70", null)]
        [InlineData("GT:DP:DPF:AD:GQ", "1/1:70:3", null)]//dropped fields without '.'
        [InlineData("GQ", ".", null)]
        [InlineData("GQX", "", null)]
        [InlineData("GQX", "./.", null)]
        public void GenotypeQuality(string formatCol, string sampleCol, int? expectedGenotypeQuality)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedGenotypeQuality = sample?.GenotypeQuality;
            Assert.Equal(expectedGenotypeQuality, observedGenotypeQuality);
        }

        [Theory]
        [InlineData("GT:TIR:TAR:DP:DPF:AD", "1/1:22,22:3,4:70:3:0,70", 25)]
        [InlineData("GT:AU:CU:GU:TU:DP:DPF:AD", "1/1:10,11:20,21:30,31:40,41:70:3:0,70", 100)]
        [InlineData("GT:DPI:DP:DPF:AD", "1/1:17:70:3:0,70", 17)]
        [InlineData("GT:DP:DPF:AD", "1/1:70:3:0,70", 70)]
        [InlineData("GT:AU:CU:GU:TU:DPF:AD", "1/1:.:20,21:30,31:40,41:3:0,70", null)]
        [InlineData("GT:AU:CU:GU:TU:DPF:AD", "1/1:.:20,21:30,31:40,41:3", null)]//dropping AD completely
        [InlineData("GT:DP:DPF:AD", "1/1:.:3:0,70", null)]
        [InlineData("DP", ".", null)]
        [InlineData("DPI", "", null)]
        public void TotalDepth(string formatCol, string sampleCol, int? expectedTotalDepth)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);

            var sample = samples[0];
            var observedTotalDepth = sample?.TotalDepth;
            Assert.Equal(expectedTotalDepth, observedTotalDepth);
        }

        [Fact]
        public void PiscesTotalDepth()
        {
            const string vcfLine =
                "chr1\t115251293\t.\tGA\tG\t100\tSB;LowVariantFreq\tDP=7882\tGT:GQ:AD:VF:NL:SB:GQX\t0/1:100:7588,294:0:20:-100.0000:100";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1),null);

            var sample = samples[0];
            var observedTotalDepth = sample.TotalDepth;
            const int expectedTotalDepth = 7882;
            Assert.Equal(expectedTotalDepth, observedTotalDepth);
        }

        [Theory]
        [InlineData("T", "GT:GQ:GQX:DP:DPF:AD:VF", "1/1:208:47:70:3:0,70:0.75", "0.75")] // VF
        [InlineData("T", "GT:TIR:TAR", "1/1:10,11:20,21", "0.3333")]                     // TAR/TIR        
        [InlineData("A", "GT:AU:CU:GU:TU", "1/1:10,11:20,21:30,31:40,41", "0.1")]        // allele counts (A)
        [InlineData("C", "GT:AU:CU:GU:TU", "1/1:10,11:20,21:30,31:40,41", "0.2")]        // allele counts (C)
        [InlineData("G", "GT:AU:CU:GU:TU", "1/1:10,11:20,21:30,31:40,41", "0.3")]        // allele counts (G)
        [InlineData("T", "GT:AU:CU:GU:TU", "1/1:10,11:20,21:30,31:40,41", "0.4")]        // allele counts (T)
        [InlineData("T", "GT:AD", "1/1:3,70", "0.9589")]                                 // allele depths
        [InlineData("T", "GT:AU:CU:GU:TU:AD", "1/1:.:20,21:30,31:40,41:7,11", "0.6111")] // missing allele count
        [InlineData("T", "GT:AD:DP:VF", "0/1:317,200:517:0.38685", "0.3869")]            // VF (rounding issue)
        public void VariantFrequency_Nominal(string altAllele, string formatCol, string sampleCol, string expectedResults)
        {
            string vcfLine = $"chr1\t5592503\t.\tC\t{altAllele}\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);

            var sample = samples[0];
            Assert.NotNull(sample?.VariantFrequencies);
            var observedResults = string.Join(',', sample.VariantFrequencies.Select(x => x.ToString("0.####")));
            Assert.Equal(expectedResults, observedResults);
        }

        [Theory]
        [InlineData("C", "T", "GT:AD", "1/1:.")]                                        // missing AD
        [InlineData("C", "T", "VF", ".")]                                               // missing VF
        [InlineData("C", "T", "AD", "")]                                                // missing AD
        [InlineData("C", "T,A", "GT:GQ:GQX:DP:DPF:AD:VF", "1/1:208:47:70:3:0,70:0.75")] // multiple alleles (VF)
        [InlineData("CG", "T", "GT:AU:CU:GU:TU", "1/1:10,11:20,21:30,31:40,41")]        // multiple ref bases (AC)
        [InlineData("CG", "T", "GT:AU:CU:GU:TU", "1/1")]                                // dropping all fields after GT
        [InlineData("C", ".", "DP:AU:CU:GU:TU", "19:0,0:14,14:0,0:5,6")]                // ref minor (AC)
        [InlineData("C", ".", "DP:AU:CU:GU:TU", "75:0,0:72,77:0,0:0,2")]                // ref minor (AC)
        public void VariantFrequency_ReturnNull(string refAllele, string altAllele, string formatCol, string sampleCol)
        {
            var vcfLine = $"chr1\t5592503\t.\t{refAllele}\t{altAllele}\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(altAllele.Split(',').Length), null);

            Assert.Single(samples);
            var sample = samples[0];
            Assert.Null(sample.VariantFrequencies);
        }

        [Theory]
        [InlineData("GT:GQ:GQX:DP:DPF:AD:VF", "1/1:208:47:70:3:70", "VF")]
        [InlineData("GT:GQ:GQX:DP:DPF:AD:VF", "1/1:208:47:70:3", "AD")]
        [InlineData("GT:DP:DPF:AD:VF:GQ:GQX", "1/1:70:3:208:47", "GQ")]
        [InlineData("GT:DP:DPF:AD:VF:GQ:GQX", "1/1:70:3:208:47", "GQX")]
        [InlineData("GT:AD:VF:FT", "1/1:47:70", "FT")]
        public void Leftout_fields_return_null(string formatCol, string sampleCol, string missingField)
        {
            var vcfLine = $"chr1\t5592503\t.\tA\tC\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1),null);

            Assert.Single(samples);
            var sample = samples[0];

            switch (missingField)
            {
                case "VF":
                    Assert.Null(sample.VariantFrequencies);
                    break;
                case "AD":
                    Assert.Null(sample.AlleleDepths);
                    break;
                case "FT":
                    Assert.False(sample.FailedFilter);
                    break;
                case "GQ":
                case "GQX":
                    Assert.Null(sample.GenotypeQuality);
                    break;

            }

        }

        [Fact]
        public void MajorChromosomeCopyTest()
        {
            // data from NIR-1095
            // for NIR-1218
            const string vcfLine = "1	9314202	Canvas:GAIN:1:9314202:9404148	N	<CNV>	36	PASS	SVTYPE=CNV;END=9404148;ensembl_gene_id=ENSG00000049239,ENSG00000252841,ENSG00000171621	RC:BC:CN:MCC	.	151:108:6:4";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1),null);

            Assert.Equal(2, samples.Length);

            var sample = samples[1];

            var observedMcc = sample?.IsLossOfHeterozygosity;
            Assert.False(observedMcc);
        }

        [Fact]
        public void EmptySamples()
        {
            // for NIR-1306
            const string vcfLine = "chrX	2735147	.	G	A	38.25	VQSRTrancheSNP99.90to100.00	AC=3;AF=0.500;AN=6;BaseQRankSum=-0.602;DP=56;Dels=0.00;FS=30.019;HaplotypeScore=7.7259;MLEAC=3;MLEAF=0.500;MQ=41.18;MQ0=0;MQRankSum=0.098;QD=1.06;ReadPosRankSum=0.266;SB=-8.681e-03;VQSLOD=-6.0901;culprit=QD	GT:AD:DP:GQ:PL	0:7,0:7:3:0,3,39	./.	0/1:14,3:17:35:35,0,35	1/1:9,10:19:3:41,3,0";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Equal(4, samples.Length);

            var sample = samples[1];
            var observedGenotype = sample.Genotype;
            var observedVariantFrequency = sample.VariantFrequencies;

            Assert.Equal("./.", observedGenotype);
            Assert.Null(observedVariantFrequency);
        }

        [Theory]
        [InlineData("GT:TIR:TAR", "1/1:0,11:0,21", "0")]
        [InlineData("GT:AU:CU:GU:TU", "1/1:0,11:0,21:0,31:0,41", "0")]
        [InlineData("GT:AD", "1/1:0,0", "0")]
        [InlineData("GT:AU:CU:GU:TU:AD", "1/1:.:20,21:30,31:40,41:0,0", "0")]
        [InlineData("GT:AD", "1/1:.", null)]
        [InlineData("VF", ".", null)]
        [InlineData("AD", "", null)]
        public void VariantFrequencyNan(string formatCol, string sampleCol, string expectedResults)
        {
            // NIR-1338
            var vcfLine = $"chr1\t5592503\t.\tC\tT\t900.00\tPASS\t.\t{formatCol}\t{sampleCol}";
            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);

            var sample = samples[0];
            if (expectedResults == null)
            {
                Assert.Null(sample?.VariantFrequencies);
                return;
            }

            Assert.NotNull(sample?.VariantFrequencies);
            var observedResults = string.Join(',', sample.VariantFrequencies.Select(x => x.ToString("0.####")));
            Assert.Equal(expectedResults, observedResults);
        }

        [Fact]
        public void SplitReadCounts()
        {
            const string vcfLine = "chr7	127717248	MantaINV:267944:0:1:2:0:0	T	<INV>	.	PASS	END=140789466;SVTYPE=INV;SVLEN=13072218;INV5	PR:SR	78,0:65,0	157,42:252,63";

            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Equal(2, samples.Length);
            var sample1 = samples[0];
            Assert.Equal(new[] { 78, 0 }, sample1.PairedEndReadCounts);
            Assert.Equal(new[] { 65, 0 }, sample1.SplitReadCounts);

            var sample2 = samples[1];
            Assert.Equal(new[] { 157, 42 }, sample2.PairedEndReadCounts);
            Assert.Equal(new[] { 252, 63 }, sample2.SplitReadCounts);
        }

        [Fact]
        public void EmptySample()
        {
            const string vcfLine = "chr7	127717248	MantaINV:267944:0:1:2:0:0	T	<INV>	.	PASS	END=140789466;SVTYPE=INV;SVLEN=13072218;INV5	PR:SR	.";

            var vcfColumns = vcfLine.Split('\t');

            var samples = vcfColumns.ToSamples(new FormatIndices(), GetSimplePositionUsingAlleleNum(1), null);

            Assert.Single(samples);
            var sample = samples[0];
            Assert.True(sample.IsEmpty);
        }

    }
}