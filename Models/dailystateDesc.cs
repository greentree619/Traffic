namespace Traffic.Models
{
  public class dailystateDesc : dailystate
  {
    public string JobDesc { get; set; }

    public dailystateDesc(dailystate state)
    {
      this.id = state.id;
      this.job_id = state.job_id;
      this.state = state.state;
      this.predict_time = state.predict_time;
      this.start_time = state.start_time;
      this.end_time = state.end_time;
      this.isrunning = state.isrunning;
      this.runner_id = state.runner_id;
      this.consfailcount = state.consfailcount;
      this.isoldjourney = state.isoldjourney;
    }
  }
}
