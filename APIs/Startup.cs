using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Servicios.BLL;
using AutoMapper;
using DTO.Mappers;
using BLL;
using BLL.Contracts;
using Dominio;
using System.Data.SqlClient;

namespace APIs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<ITokenService, TokenService>();

            var connectionStringNormal = Configuration.GetConnectionString("GastroGestion");
            var connectionStringSeguridad = Configuration.GetConnectionString("GastroGestion_Seguridad");
            services.AddSingleton(new SqlConnection(connectionStringNormal));
            services.AddSingleton(new SqlConnection(connectionStringSeguridad));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token"])),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}


//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.HttpsPolicy;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using Servicios.BLL;
//using AutoMapper;
//using DTO.Mappers;
//using BLL;
//using BLL.Contracts;
//using Dominio;
//using System.Data.SqlClient;

//namespace APIs
//{
//    public class Startup
//    {
//        public Startup(IConfiguration configuration)
//        {
//            Configuration = configuration;
//        }

//        public IConfiguration Configuration { get; }

//        // This method gets called by the runtime. Use this method to add services to the container.
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddControllers();


//            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

//            services.AddScoped<IAuthRepository, AuthRepository>();

//            //Se agrega token service
//            services.AddScoped<ITokenService, TokenService>();

//            //Configuracion BDD
//            var connectionStringNormal = Configuration.GetConnectionString("GastroGestion");
//            var connectionStringSeguridad = Configuration.GetConnectionString("GastroGestion_Seguridad");
//            services.AddSingleton(new SqlConnection(connectionStringNormal));
//            services.AddSingleton(new SqlConnection(connectionStringSeguridad));

//            //Configuracion para uso del token

//            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(optiones =>
//            {
//                optiones.TokenValidationParameters = new TokenValidationParameters
//                {
//                    ValidateIssuerSigningKey = true,
//                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token"])),
//                    //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Aguante River")),
//                    ValidateIssuer = false,
//                    ValidateAudience = false
//                };
//            });
//        }

//        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//        {
//            if (env.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//            }

//            app.UseHttpsRedirection();

//            app.UseRouting();

//            //Agregar Authetication
//            app.UseAuthentication();


//            app.UseAuthorization();

//            app.UseEndpoints(endpoints =>
//            {
//                endpoints.MapControllers();
//            });
//        }
//    }
//}
