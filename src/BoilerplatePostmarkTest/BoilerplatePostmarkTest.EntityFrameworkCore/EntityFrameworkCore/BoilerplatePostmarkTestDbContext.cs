using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BoilerplatePostmarkTest.EntityFrameworkCore
{
    public class BoilerplatePostmarkTestDbContext : AbpDbContext
    {
        //Add DbSet properties for your entities...

        public BoilerplatePostmarkTestDbContext(DbContextOptions<BoilerplatePostmarkTestDbContext> options) 
            : base(options)
        {

        }
    }
}
