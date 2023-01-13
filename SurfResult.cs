namespace Traffic
{
  public class SurfResult
  {
    public bool IsSuccess;
    public bool IsGoogle;
    public int nBrandKind;
    public int nRanks;
    public int FailReason;

    public SurfResult()
    {
      this.nBrandKind = 0;
      this.nRanks = 0;
    }
  }
}
