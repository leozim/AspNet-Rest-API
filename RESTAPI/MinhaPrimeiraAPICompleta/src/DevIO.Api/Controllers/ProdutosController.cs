using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using DevIO.Api.DTO;
using DevIO.Business.Intefaces;
using DevIO.Business.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DevIO.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProdutosController : MainController
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoService _produtoService;
        private readonly IMapper _mapper;

        public ProdutosController(INotificador notificador,
            IProdutoRepository produtoRepository,
            IProdutoService produtoService,
            IMapper mapper) : base(notificador)
        {
            _produtoRepository = produtoRepository;
            _produtoService = produtoService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetAll()
        {
            var result = _mapper.Map<IEnumerable<ProdutoDTO>>(await _produtoRepository.ObterProdutosFornecedores());
            return CustomResponse(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProdutoDTO>> GetById(Guid id)
        {
            var result = await GetProdutoById(id);

            if (result == null) return NotFound();

            return CustomResponse(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProdutoDTO>> Post([FromBody] ProdutoDTO produtoDto)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var imagemNome = Guid.NewGuid() + "_" + produtoDto.Imagem;

            if (!UploadArquivo(produtoDto.ImagemUpload, imagemNome))
            {
                return CustomResponse(produtoDto);
            }

            produtoDto.Imagem = imagemNome;
            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoDto));

            return CustomResponse(produtoDto);
        }

        [HttpPut("id:guid")]
        public async Task<ActionResult<ProdutoDTO>> Put(Guid id, [FromBody] ProdutoDTO produtoDto)
        {
            if (id != produtoDto.Id) return BadRequest();

            if (!ModelState.IsValid) return CustomResponse(produtoDto);

            var produtoToUpdate = await GetProdutoById(id);
            produtoDto.Imagem = produtoToUpdate.Imagem; // mantem o nome da imagem como ja esta no banco
            if (produtoDto.ImagemUpload != null)
            {
                var imagemNome = Guid.NewGuid() + "_" + produtoDto.Imagem;
                if (!UploadArquivo(produtoDto.ImagemUpload, imagemNome))
                {
                    return CustomResponse(ModelState);
                }

                produtoToUpdate.Imagem = imagemNome;
            }

            produtoToUpdate.Nome = produtoDto.Nome;
            produtoToUpdate.Descricao = produtoDto.Descricao;
            produtoToUpdate.Valor = produtoDto.Valor;
            produtoToUpdate.Ativo = produtoToUpdate.Ativo;

            await _produtoService.Atualizar(_mapper.Map<Produto>(produtoToUpdate));

            return CustomResponse(produtoDto);
        }

        [RequestSizeLimit(40000000)]
        [HttpPost("Adicionar")]
        public async Task<ActionResult<ProdutoImagemDTO>> FormFilePost(ProdutoImagemDTO produtoDto)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var imagemPrefixo = Guid.NewGuid() + "_";

            if (!await UploadArquivoAlternativo(produtoDto.ImagemUpload, imagemPrefixo))
            {
                return CustomResponse(ModelState);
            }

            produtoDto.Imagem = imagemPrefixo + produtoDto.ImagemUpload.FileName;
            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoDto));

            return CustomResponse(produtoDto);
        }

        [HttpDelete("id:guid")]
        public async Task<ActionResult<ProdutoDTO>> Delete(Guid id)
        {
            var produtoDto = await GetProdutoById(id);

            if (produtoDto == null) return NotFound(id);

            await _produtoService.Remover(id);

            return CustomResponse(produtoDto);
        }

        private async Task<ProdutoDTO> GetProdutoById(Guid id)
        {
            return _mapper.Map<ProdutoDTO>(await _produtoRepository.ObterPorId(id));
        }

        private bool UploadArquivo(string arquivo, string imgNome)
        {
            var imageDataByteArray = Convert.FromBase64String(arquivo);

            if (string.IsNullOrEmpty(arquivo))
            {
                //ModelState.AddModelError(string.Empty, "Já existe um arquivo com este nome!");
                NotificarErro("Arquivo de imagem vazio");
                return false;
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgNome);

            if (System.IO.File.Exists(filePath))
            {
                NotificarErro("Já existe um arquivo com este nome!");
                return false;
            }

            System.IO.File.WriteAllBytes(filePath, imageDataByteArray);

            return true;
        }

        private async Task<bool> UploadArquivoAlternativo(IFormFile arquivo, string imgPrefixo)
        {
            if (arquivo == null || arquivo.Length <= 0)
            {
                NotificarErro("Forneça uma imagem para este produto!");
                return false;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgPrefixo + arquivo.FileName);
            var retornaOQue = arquivo.Name;

            if (System.IO.File.Exists(path))
            {
                NotificarErro("Já existe um arquivo com este nome!");
                return false;
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }
            
            return true;
        }

    }
}