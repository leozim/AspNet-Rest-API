using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using DevIO.Api.DTO;
using DevIO.Business.Intefaces;
using DevIO.Business.Models;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.Api.Controllers
{
    [Route("api/[controller]")]
    public class FornecedoresController : MainController
    {
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly IFornecedorService _fornecedorService;
        private readonly IEnderecoRepository _enderecoRepository;
        private readonly IMapper _mapper;

        public FornecedoresController(IFornecedorRepository fornecedorRepository, 
            IFornecedorService fornecedorService,
            IEnderecoRepository enderecoRepository,
            IMapper mapper, 
            INotificador notificador): base(notificador)
        {
            this._fornecedorRepository = fornecedorRepository;
            this._fornecedorService = fornecedorService;
            this._enderecoRepository = enderecoRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FornecedorDTO>>> GetAll()
        {
            var fornecedores = await GetFornecedoresDTO();
            return CustomResponse(fornecedores);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FornecedorDTO>> GetById(Guid id)
        {
            var fornecedor = await GetFornecedorDTOBy(id);

            return fornecedor == null ? NotFound() : CustomResponse(fornecedor);
        }

        [HttpGet("obter-endereco/{id:guid}")]
        public async Task<ActionResult<EnderecoDTO>> GetEnderecoBy(Guid id)
        {
            return  _mapper.Map<EnderecoDTO>(await this._enderecoRepository.ObterPorId(id));
        }

        [HttpPost]
        public async Task<ActionResult<FornecedorDTO>> Post([FromBody] FornecedorDTO fornecedorDTO)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            await this._fornecedorService.Adicionar(_mapper.Map<Fornecedor>(fornecedorDTO));

            return CustomResponse(fornecedorDTO);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<FornecedorDTO>> Put(Guid id, [FromBody] FornecedorDTO fornecedorDTO)
        {
            if (id != fornecedorDTO.Id)
            {
                NotificarErro("O id informado não é o mesmo que foi passado na query");
                return CustomResponse(fornecedorDTO);
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            await this._fornecedorService.Atualizar(_mapper.Map<Fornecedor>(fornecedorDTO));

            return CustomResponse(fornecedorDTO);
        }

        [HttpPut("atualizar-endereco/{id:guid}")]
        public async Task<ActionResult<EnderecoDTO>> Put(Guid id, [FromBody] EnderecoDTO enderecoDTO)
        {
            if (id != enderecoDTO.Id)
            {
                return BadRequest(enderecoDTO);
            }

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            await this._enderecoRepository.Atualizar(_mapper.Map<Endereco>(enderecoDTO));

            return CustomResponse(enderecoDTO);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<FornecedorDTO>> Delete(Guid id)
        {
            var fornecedor = await GetFornecedorDTOEnderecoBy(id);

            if (fornecedor == null) return NotFound();

            await _fornecedorService.Remover(id);

            return NoContent();
        }

        private async Task<FornecedorDTO> GetFornecedorDTOBy(Guid id)
        {
            return _mapper.Map<FornecedorDTO>(await this._fornecedorRepository.ObterPorId(id));
        }

        private async Task<FornecedorDTO> GetFornecedorDTOEnderecoBy(Guid id)
        {
            return _mapper.Map<FornecedorDTO>(await this._fornecedorRepository.ObterFornecedorProdutosEndereco(id));
        }

        private async Task<IEnumerable<FornecedorDTO>> GetFornecedoresDTO()
        {
            return _mapper.Map<IEnumerable<FornecedorDTO>>(await this._fornecedorRepository.ObterTodos());
        }
    }
}