using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using ProtoBuf;
using ProtoBuf.Grpc.Configuration;

using HCms.Content.Services;
using HCms.Content.ViewModels;


namespace HCms.Web.Services
{

	[ProtoContract]
	public class DocumentGrpcRequest
	{
		[ProtoMember(1)]
		public int Id { get; set; }

		[ProtoMember(2)]
		public string Root { get; set; }

		[ProtoMember(3)]
		public string Path { get; set; }

		[ProtoMember(4)]
		public string PathMapper { get; set; }

		[ProtoMember(5)]
		public bool Siblings { get; set; }

		[ProtoMember(6)]
		[DefaultValue(-1)]
		public int ChildrenFromPos { get; set; } = -1;

		[ProtoMember(7)]
		[DefaultValue(1000)]
		public int TakeChildren { get; set; } = 1000;

		[ProtoMember(8)]
		public int[] AllowedStatus { get; set; }
	}



	[ProtoContract]
	public class ListGrpcRequest
	{
		[ProtoMember(1)]
		public int Id { get; set; }

		[ProtoMember(2)]
		public string PathMapper { get; set; }
	}



	[ProtoContract]
	public class DocumentGrpcResult
	{
		[ProtoMember(1)]
		public int Status { get; set; }

		[ProtoMember(2)]
		public byte[] Data { get; set; }
	}



	[Service("HCms.Content.ContentGrpcService")]
	public interface IContentGrpcService
	{
		Task<DocumentGrpcResult> GetDocument(DocumentGrpcRequest request, CancellationToken ct);
		Task<DocumentGrpcResult> GetList(ListGrpcRequest request, CancellationToken ct);
	}



	[Authorize(Policy = "IsConsumerApp")]
	public class ContentGrpcService(IServiceProvider serviceProvider, IPathMapperFactory pathMapperFactory) : IContentGrpcService
	{
		readonly IServiceProvider _serviceProvider = serviceProvider;
		readonly IPathMapperFactory _pathMapperFactory = pathMapperFactory;

		public async Task<DocumentGrpcResult> GetDocument(DocumentGrpcRequest request, CancellationToken ct)
		{
			using var scope = _serviceProvider.CreateScope();
			var cps = scope.ServiceProvider.GetRequiredService<ContentProvidingService>();
			var pm = _pathMapperFactory.Get(request.PathMapper);

			Document doc;

			if (request.Id != 0)
				doc = await cps.GetDocument(pm, request.Id, request.ChildrenFromPos, request.TakeChildren, request.Siblings, request.AllowedStatus, ct);
			else
				doc = await cps.GetDocument(pm, request.Root, request.Path, request.ChildrenFromPos, request.TakeChildren, request.Siblings, request.AllowedStatus, false, ct);

			var result = new DocumentGrpcResult()
			{
				Status = doc != null ? 200 : 404,
				Data = MessagePack.MessagePackSerializer.Serialize(doc, cancellationToken: ct)
			};

			return result;
		}

		public async Task<DocumentGrpcResult> GetList(ListGrpcRequest request, CancellationToken ct)
		{
			using var scope = _serviceProvider.CreateScope();
			var cps = scope.ServiceProvider.GetRequiredService<ContentProvidingService>();
			var pm = _pathMapperFactory.Get(request.PathMapper);

			var docs = await cps.ListDocuments(pm, request.Id, ct);

			var result = new DocumentGrpcResult()
			{
				Status = docs != null ? 200 : 404,
				Data = MessagePack.MessagePackSerializer.Serialize(docs, cancellationToken: ct)
			};

			return result;
		}
	}

}