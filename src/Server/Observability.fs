module Observability

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open OpenTelemetry.Metrics
open OpenTelemetry.Trace
open Npgsql

let addOpenTelemetry (services: IServiceCollection) : unit =
    services.AddOpenTelemetry()
    |> fun otel ->
        otel.WithMetrics(fun metrics ->
            metrics
                .AddView(
                    "http.server.request.duration",
                    new MetricStreamConfiguration(TagKeys = [| "http.route" |])
                )
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter()
            |> ignore
        )
    |> fun otel ->
        otel.WithTracing(fun tracing ->
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddEntityFrameworkCoreInstrumentation(fun efCoreOpts ->
                    efCoreOpts.SetDbStatementForText <- true
                )
                .AddOtlpExporter()
            |> ignore
        )
    |> ignore

let addPrometheusEndpoint (app: WebApplication) : unit =
    app.UseOpenTelemetryPrometheusScrapingEndpoint() |> ignore
