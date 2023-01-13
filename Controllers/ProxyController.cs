using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Traffic.Data;
using Traffic.Models;

namespace Traffic.Controllers
{
  public class Proxy : Controller
  {
    private readonly ApplicationDbContext _context;

    public Proxy(ApplicationDbContext context) => this._context = context;

    public async Task<IActionResult> Index()
    {
      Proxy proxy = this;
      List<location> listAsync = await EntityFrameworkQueryableExtensions.ToListAsync<location>((IQueryable<location>) proxy._context.location, new CancellationToken());
      return (IActionResult) proxy.View((object) listAsync);
    }

    public async Task<IActionResult> Details(int? id)
    {
      Proxy proxy = this;
      if (!id.HasValue)
        return (IActionResult) proxy.NotFound();
      location model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<location>((IQueryable<location>) proxy._context.location, (Expression<Func<location, bool>>) (m => (int?) m.id == id), new CancellationToken());
      return model != null ? (IActionResult) proxy.View((object) model) : (IActionResult) proxy.NotFound();
    }

    public IActionResult Create() => (IActionResult) this.View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(new string[] {"id,depth,parentid,label"})] location location)
    {
      Proxy proxy = this;
      if (!proxy.ModelState.IsValid)
        return (IActionResult) proxy.View((object) location);
      proxy._context.Add<location>(location);
      int num = await proxy._context.SaveChangesAsync(new CancellationToken());
      return (IActionResult) proxy.RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
      Proxy proxy = this;
      if (!id.HasValue)
        return (IActionResult) proxy.NotFound();
      location model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<location>((IQueryable<location>) proxy._context.location, (Expression<Func<location, bool>>) (m => (int?) m.id == id), new CancellationToken());
      return model != null ? (IActionResult) proxy.View((object) model) : (IActionResult) proxy.NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(new string[] {"id,depth,parentid,label"})] location location)
    {
      Proxy proxy = this;
      if (id != location.id)
        return (IActionResult) proxy.NotFound();
      if (!proxy.ModelState.IsValid)
        return (IActionResult) proxy.View((object) location);
      try
      {
        proxy._context.Update<location>(location);
        int num = await proxy._context.SaveChangesAsync(new CancellationToken());
      }
      catch (DbUpdateConcurrencyException ex)
      {
        if (!proxy.locationExists(location.id))
          return (IActionResult) proxy.NotFound();
        throw;
      }
      return (IActionResult) proxy.RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int? id)
    {
      Proxy proxy = this;
      if (!id.HasValue)
        return (IActionResult) proxy.NotFound();
      location model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<location>((IQueryable<location>) proxy._context.location, (Expression<Func<location, bool>>) (m => (int?) m.id == id), new CancellationToken());
      return model != null ? (IActionResult) proxy.View((object) model) : (IActionResult) proxy.NotFound();
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      Proxy proxy = this;
      location entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<location>((IQueryable<location>) proxy._context.location, (Expression<Func<location, bool>>) (m => m.id == id), new CancellationToken());
      proxy._context.location.Remove(entity);
      int num = await proxy._context.SaveChangesAsync(new CancellationToken());
      return (IActionResult) proxy.RedirectToAction("Index");
    }

    private bool locationExists(int id) => this._context.location.Any<location>((Expression<Func<location, bool>>) (e => e.id == id));
  }
}
