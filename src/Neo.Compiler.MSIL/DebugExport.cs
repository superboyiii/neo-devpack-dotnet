using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Neo.Compiler
{
    public static class DebugExport
    {
        private static MyJson.JsonNode_Array GetSequencePoints(IEnumerable<NeoCode> codes, IReadOnlyDictionary<string, int> docMap, IReadOnlyDictionary<int, int> addrConvTable)
        {
            var points = codes
                .Where(code => code.sequencePoint != null)
                .Select(code => (code.addr, code.sequencePoint));

            var outjson = new MyJson.JsonNode_Array();

            foreach (var (address, sequencePoint) in points)
            {
                var value = string.Format("{0}[{1}]{2}:{3}-{4}:{5}",
                    addrConvTable.TryGetValue(address, out var newAddress) ? newAddress : address,
                    docMap[sequencePoint.Document.Url],
                    sequencePoint.StartLine,
                    sequencePoint.StartColumn,
                    sequencePoint.EndLine,
                    sequencePoint.EndColumn);
                outjson.Add(new MyJson.JsonNode_ValueString(value));
            }

            return outjson;
        }

        private static MyJson.JsonNode_Array ConvertParamList(IEnumerable<NeoParam> @params)
        {
            @params ??= Enumerable.Empty<NeoParam>();
            var paramsJson = new MyJson.JsonNode_Array();
            foreach (var param in @params)
            {
                var value = string.Format("{0},{1}", param.name, FuncExport.ConvType(param.type));
                paramsJson.Add(new MyJson.JsonNode_ValueString(value));
            }

            return paramsJson;
        }

        private static MyJson.JsonNode_Array GetMethods(NeoModule module, IReadOnlyDictionary<string, int> docMap, IReadOnlyDictionary<int, int> addrConvTable)
        {
            var outjson = new MyJson.JsonNode_Array();

            foreach (var method in module.mapMethods.Values)
            {
                if (method.body_Codes.Values.Count == 0)
                {
                    continue;
                }

                var name = string.Format("{0},{1}",
                    method._namespace, method.displayName);

                var range = string.Format("{0}-{1}",
                    method.body_Codes.Values.First().addr,
                    method.body_Codes.Values.Last().addr);

                var methodJson = new MyJson.JsonNode_Object();
                methodJson.SetDictValue("id", method.name);
                methodJson.SetDictValue("name", name);
                methodJson.SetDictValue("range", range);
                methodJson.SetDictValue("params", ConvertParamList(method.paramtypes));
                methodJson.SetDictValue("return", FuncExport.ConvType(method.returntype));
                methodJson.SetDictValue("variables", ConvertParamList(method.method?.body_Variables));
                methodJson.SetDictValue("sequence-points", GetSequencePoints(method.body_Codes.Values, docMap, addrConvTable));
                outjson.Add(methodJson);
            }
            return outjson;
        }

        private static MyJson.JsonNode_Array GetEvents(NeoModule module)
        {
            var outjson = new MyJson.JsonNode_Array();
            foreach (var @event in module.mapEvents.Values)
            {
                var name = string.Format("{0},{1}",
                    @event._namespace, @event.displayName);

                var eventJson = new MyJson.JsonNode_Object();
                eventJson.SetDictValue("id", @event.name);
                eventJson.SetDictValue("name", name);
                eventJson.SetDictValue("params", ConvertParamList(@event.paramtypes));
                outjson.Add(eventJson);
            }
            return outjson;
        }

        private static IReadOnlyDictionary<string, int> GetDocumentMap(NeoModule module)
        {
            return module.mapMethods.Values
                .SelectMany(m => m.body_Codes.Values)
                .Where(code => code.sequencePoint != null)
                .Select(code => code.sequencePoint.Document.Url)
                .Distinct()
                .Select((d, i) => (d, i))
                .ToDictionary(t => t.d, t => t.i);
        }

        private static MyJson.JsonNode_Array GetDocuments(IReadOnlyDictionary<string, int> docMap)
        {
            var outjson = new MyJson.JsonNode_Array();
            foreach (var doc in docMap.OrderBy(kvp => kvp.Value))
            {
                Debug.Assert(outjson.Count == doc.Value);
                outjson.Add(new MyJson.JsonNode_ValueString(doc.Key));
            }
            return outjson;
        }

        public static MyJson.JsonNode_Object Export(NeoModule module, byte[] script, IReadOnlyDictionary<int, int> addrConvTable)
        {
            var docMap = GetDocumentMap(module);
            addrConvTable ??= ImmutableDictionary<int, int>.Empty;

            var outjson = new MyJson.JsonNode_Object();
            outjson.SetDictValue("hash", FuncExport.ComputeHash(script));
            // outjson.SetDictValue("entrypoint", module.mainMethod);
            outjson.SetDictValue("documents", GetDocuments(docMap));
            outjson.SetDictValue("methods", GetMethods(module, docMap, addrConvTable));
            outjson.SetDictValue("events", GetEvents(module));
            return outjson;
        }
    }
}
