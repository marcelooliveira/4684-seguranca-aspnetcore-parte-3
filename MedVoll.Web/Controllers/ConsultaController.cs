using MedVoll.Web.Dtos;
using MedVoll.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedVoll.Web.Controllers
{
    [Authorize]
    [Route("consultas")]
    public class ConsultaController : BaseController
    {
        private const string PaginaListagem = "Listagem";
        private const string PaginaCadastro = "Formulario";

        private readonly IMedVollApiService _medVollApiService;

        public ConsultaController(IMedVollApiService medVollApiService)
        : base()
        {
            _medVollApiService = medVollApiService;
        }

        [HttpGet]
        [Route("{page?}")]
        public async Task<IActionResult> ListarAsync([FromQuery] int page = 1)
        {
            PaginatedList<ConsultaDto> consultas = await _medVollApiService.WithContext(HttpContext).ListarConsultas(page);
            ViewBag.Consultas = consultas;
            ViewData["Url"] = "Consultas";
            return View(PaginaListagem, consultas);
        }

        [HttpGet]
        [Route("formulario/{id?}")]
        public async Task<IActionResult> ObterFormularioAsync(long id = 0)
        {
            FormularioConsultaDto formularioConsulta = await _medVollApiService.WithContext(HttpContext).ObterFormularioConsulta(id);
            ViewData["Medicos"] = formularioConsulta.Medicos;
            return View(PaginaCadastro, formularioConsulta.Consulta);
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> SalvarAsync([FromForm] ConsultaDto dados)
        {
            if (dados._method == "delete")
            {
                await _medVollApiService.WithContext(HttpContext).ExcluirConsulta(dados.Id);
                return Redirect("/consultas");
            }

            if (!ModelState.IsValid)
            {
                PaginatedList<MedicoDto> medicos = await _medVollApiService.WithContext(HttpContext).ListarMedicos(1);
                ViewData["Medicos"] = medicos.Items;
                return View(PaginaCadastro, dados);
            }

            try
            {
                await _medVollApiService.WithContext(HttpContext).SalvarConsulta(dados);
                return Redirect("/consultas");
            }
            catch (Exception ex)
            {
                ViewBag.Erro = ex.Message;
                ViewBag.Dados = dados;
                return View(PaginaCadastro);
            }
        }
    }
}
