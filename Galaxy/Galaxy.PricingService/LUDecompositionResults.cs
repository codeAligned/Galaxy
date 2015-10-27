
namespace PricingLib
{
   
    public class LUDecompositionResults
    {
        public Matrix L { get; set; }
        public Matrix U { get; set; }
        public int[] PivotArray { get; set; }

        public LUDecompositionResults() { }

        public LUDecompositionResults(Matrix matL, Matrix matU, int[] nPivotArray)
        {
            L = matL; U = matU; PivotArray = nPivotArray;
        }
    }
}
