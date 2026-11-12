using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alcedo.Framework.Sample.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "alcedoadmin");

            migrationBuilder.EnsureSchema(
                name: "alcedoformulaengine");

            migrationBuilder.EnsureSchema(
                name: "alcedoidentity");

            migrationBuilder.EnsureSchema(
                name: "masstransit");

            migrationBuilder.EnsureSchema(
                name: "juicestore");

            migrationBuilder.EnsureSchema(
                name: "alcedoframework");

            migrationBuilder.EnsureSchema(
                name: "alcedonotification");

            migrationBuilder.EnsureSchema(
                name: "quartz");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "access_log_entities",
                schema: "alcedoadmin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trace_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    user_agent = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    method = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    area = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    controller = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    url = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    execution_ms = table.Column<int>(type: "integer", nullable: true),
                    exception = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_log_entities", x => x.id);
                },
                comment: "access_log_entities");

            migrationBuilder.CreateTable(
                name: "formula_agg_roots",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formula_agg_roots", x => x.id);
                },
                comment: "formula_agg_roots");

            migrationBuilder.CreateTable(
                name: "formula_execution_log_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fomula_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    formula_version = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    input_json = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    output_json = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formula_execution_log_entities", x => x.id);
                },
                comment: "formula_execution_log_entities");

            migrationBuilder.CreateTable(
                name: "history_formula_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    original_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_formula_entities", x => x.id);
                },
                comment: "history_formula_entities");

            migrationBuilder.CreateTable(
                name: "history_user_entities",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    login_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    user_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    contact = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    join_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    operator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_user_entities", x => x.id);
                },
                comment: "history_user_entities");

            migrationBuilder.CreateTable(
                name: "inbox_state",
                schema: "masstransit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    receive_count = table.Column<int>(type: "integer", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sequence_number = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_state", x => x.id);
                    table.UniqueConstraint("ak_inbox_state_message_id_consumer_id", x => new { x.message_id, x.consumer_id });
                });

            migrationBuilder.CreateTable(
                name: "juice_agg_roots",
                schema: "juicestore",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    flavor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    volume_ml = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    creator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_juice_agg_roots", x => x.id);
                },
                comment: "juice_agg_roots");

            migrationBuilder.CreateTable(
                name: "login_log_entities",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    login_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fail_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_log_entities", x => x.id);
                },
                comment: "login_log_entities");

            migrationBuilder.CreateTable(
                name: "master_data_audit_log_entities",
                schema: "alcedoframework",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_identity = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    operation_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    operated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    operator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    note = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_master_data_audit_log_entities", x => x.id);
                },
                comment: "master_data_audit_log_entities");

            migrationBuilder.CreateTable(
                name: "message_config_agg_roots",
                schema: "alcedonotification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    should_skip = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    creator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_config_agg_roots", x => x.id);
                },
                comment: "message_config_agg_roots");

            migrationBuilder.CreateTable(
                name: "message_error_entities",
                schema: "alcedonotification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload_json = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    phone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    error_msg = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_error_entities", x => x.id);
                },
                comment: "message_error_entities");

            migrationBuilder.CreateTable(
                name: "message_template_agg_roots",
                schema: "alcedonotification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    title_template = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    body_template = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    external_template_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    creator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_template_agg_roots", x => x.id);
                },
                comment: "message_template_agg_roots");

            migrationBuilder.CreateTable(
                name: "outbox_state",
                schema: "masstransit",
                columns: table => new
                {
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sequence_number = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_state", x => x.outbox_id);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_calendars",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    calendar_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    calendar = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_calendars", x => new { x.sched_name, x.calendar_name });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_fired_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    entry_id = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    instance_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    fired_time = table.Column<long>(type: "bigint", nullable: false),
                    sched_time = table.Column<long>(type: "bigint", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    job_name = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    job_group = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    is_nonconcurrent = table.Column<bool>(type: "bool", nullable: false),
                    requests_recovery = table.Column<bool>(type: "bool", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_fired_triggers", x => new { x.sched_name, x.entry_id });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_job_details",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    job_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    job_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    job_class_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    is_durable = table.Column<bool>(type: "bool", nullable: false),
                    is_nonconcurrent = table.Column<bool>(type: "bool", nullable: false),
                    is_update_data = table.Column<bool>(type: "bool", nullable: false),
                    requests_recovery = table.Column<bool>(type: "bool", nullable: false),
                    job_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_job_details", x => new { x.sched_name, x.job_name, x.job_group });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_locks",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    lock_name = table.Column<string>(type: "text", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_locks", x => new { x.sched_name, x.lock_name });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_paused_trigger_grps",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_paused_trigger_grps", x => new { x.sched_name, x.trigger_group });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_scheduler_state",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    instance_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    last_checkin_time = table.Column<long>(type: "bigint", nullable: false),
                    checkin_interval = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_scheduler_state", x => new { x.sched_name, x.instance_name });
                });

            migrationBuilder.CreateTable(
                name: "role_aggs",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    creator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_aggs", x => x.id);
                },
                comment: "role_aggs");

            migrationBuilder.CreateTable(
                name: "storage_gc",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bucket = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_gc", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_aggs",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    user_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    contact = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    requires_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    use_two_factor_authenticator = table.Column<bool>(type: "boolean", nullable: false),
                    current_session = table.Column<Guid>(type: "uuid", nullable: true),
                    join_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    is_root_user = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    creator_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updater_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updater_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_aggs", x => x.id);
                },
                comment: "user_aggs");

            migrationBuilder.CreateTable(
                name: "formula_expression_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expression = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    note = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formula_expression_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_formula_expression_entities_formula_agg_roots_formula_id",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "formula_agg_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "formula_expression_entities");

            migrationBuilder.CreateTable(
                name: "formula_input_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    var_description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    var_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    valid_condition = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    default_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formula_input_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_formula_input_entities_formula_agg_roots_formula_id",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "formula_agg_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "formula_input_entities");

            migrationBuilder.CreateTable(
                name: "formula_output_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    var_description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    var_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expression = table.Column<string>(type: "text", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formula_output_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_formula_output_entities_formula_agg_roots_formula_id",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "formula_agg_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "formula_output_entities");

            migrationBuilder.CreateTable(
                name: "history_formula_expression_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expression = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    note = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_formula_expression_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_history_formula_expression_entities_history_formula_entitie",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "history_formula_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "history_formula_expression_entities");

            migrationBuilder.CreateTable(
                name: "history_formula_input_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    var_description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    var_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    valid_condition = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    default_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_formula_input_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_history_formula_input_entities_history_formula_entities_for",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "history_formula_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "history_formula_input_entities");

            migrationBuilder.CreateTable(
                name: "history_formula_output_entities",
                schema: "alcedoformulaengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: false),
                    var_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    var_description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    var_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expression = table.Column<string>(type: "text", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_history_formula_output_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_history_formula_output_entities_history_formula_entities_fo",
                        column: x => x.formula_id,
                        principalSchema: "alcedoformulaengine",
                        principalTable: "history_formula_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "history_formula_output_entities");

            migrationBuilder.CreateTable(
                name: "message_config_template_relations_entities",
                schema: "alcedonotification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_config_template_relations_entities", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_config_template_relations_entities_message_config_a",
                        column: x => x.message_config_id,
                        principalSchema: "alcedonotification",
                        principalTable: "message_config_agg_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_config_template_relations_entities_message_template",
                        column: x => x.message_template_id,
                        principalSchema: "alcedonotification",
                        principalTable: "message_template_agg_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "message_config_template_relations_entities");

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "masstransit",
                columns: table => new
                {
                    sequence_number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enqueue_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    headers = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<string>(type: "text", nullable: true),
                    inbox_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inbox_consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    message_type = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    destination_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    response_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    fault_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message", x => x.sequence_number);
                    table.ForeignKey(
                        name: "fk_outbox_message_inbox_state_inbox_message_id_inbox_consumer_",
                        columns: x => new { x.inbox_message_id, x.inbox_consumer_id },
                        principalSchema: "masstransit",
                        principalTable: "inbox_state",
                        principalColumns: new[] { "message_id", "consumer_id" });
                    table.ForeignKey(
                        name: "fk_outbox_message_outbox_state_outbox_id",
                        column: x => x.outbox_id,
                        principalSchema: "masstransit",
                        principalTable: "outbox_state",
                        principalColumn: "outbox_id");
                });

            migrationBuilder.CreateTable(
                name: "qrtz_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    job_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    job_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    next_fire_time = table.Column<long>(type: "bigint", nullable: true),
                    prev_fire_time = table.Column<long>(type: "bigint", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: true),
                    trigger_state = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_type = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    start_time = table.Column<long>(type: "bigint", nullable: false),
                    end_time = table.Column<long>(type: "bigint", nullable: true),
                    calendar_name = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    misfire_instr = table.Column<short>(type: "smallint", nullable: true),
                    job_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_triggers_qrtz_job_details_sched_name_job_name_job_group",
                        columns: x => new { x.sched_name, x.job_name, x.job_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_job_details",
                        principalColumns: new[] { "sched_name", "job_name", "job_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions_relations",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_relations_role_aggs_role_id",
                        column: x => x.role_id,
                        principalSchema: "alcedoidentity",
                        principalTable: "role_aggs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "role_permissions_relations");

            migrationBuilder.CreateTable(
                name: "user_roles_relations",
                schema: "alcedoidentity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_relations_role_aggs_role_id",
                        column: x => x.role_id,
                        principalSchema: "alcedoidentity",
                        principalTable: "role_aggs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_relations_user_aggs_user_id",
                        column: x => x.user_id,
                        principalSchema: "alcedoidentity",
                        principalTable: "user_aggs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "user_roles_relations");

            migrationBuilder.CreateTable(
                name: "qrtz_blob_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    blob_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_blob_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_blob_triggers_qrtz_triggers_sched_name_trigger_name_tr",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_cron_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    cron_expression = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    time_zone_id = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_cron_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_cron_triggers_qrtz_triggers_sched_name_trigger_name_tr",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_simple_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    repeat_count = table.Column<long>(type: "bigint", nullable: false),
                    repeat_interval = table.Column<long>(type: "bigint", nullable: false),
                    times_triggered = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_simple_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_simple_triggers_qrtz_triggers_sched_name_trigger_name_",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_simprop_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_name = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    trigger_group = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    str_prop_1 = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    str_prop_2 = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    str_prop_3 = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    int_prop_1 = table.Column<int>(type: "integer", nullable: true),
                    int_prop_2 = table.Column<int>(type: "integer", nullable: true),
                    long_prop_1 = table.Column<long>(type: "bigint", nullable: true),
                    long_prop_2 = table.Column<long>(type: "bigint", nullable: true),
                    dec_prop_1 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    dec_prop_2 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    bool_prop_1 = table.Column<bool>(type: "bool", nullable: true),
                    bool_prop_2 = table.Column<bool>(type: "bool", nullable: true),
                    time_zone_id = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_simprop_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_simprop_triggers_qrtz_triggers_sched_name_trigger_name",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_formula_agg_roots_code",
                schema: "alcedoformulaengine",
                table: "formula_agg_roots",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formula_agg_roots_code_version",
                schema: "alcedoformulaengine",
                table: "formula_agg_roots",
                columns: new[] { "code", "version" });

            migrationBuilder.CreateIndex(
                name: "ix_formula_expression_entities_formula_id_order",
                schema: "alcedoformulaengine",
                table: "formula_expression_entities",
                columns: new[] { "formula_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formula_expression_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "formula_expression_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formula_input_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "formula_input_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formula_output_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "formula_output_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_entities_code",
                schema: "alcedoformulaengine",
                table: "history_formula_entities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_entities_code_original_version",
                schema: "alcedoformulaengine",
                table: "history_formula_entities",
                columns: new[] { "code", "original_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_expression_entities_formula_id_order",
                schema: "alcedoformulaengine",
                table: "history_formula_expression_entities",
                columns: new[] { "formula_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_expression_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "history_formula_expression_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_input_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "history_formula_input_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_history_formula_output_entities_formula_id_var_name",
                schema: "alcedoformulaengine",
                table: "history_formula_output_entities",
                columns: new[] { "formula_id", "var_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_state_delivered",
                schema: "masstransit",
                table: "inbox_state",
                column: "delivered");

            migrationBuilder.CreateIndex(
                name: "ix_juice_agg_roots_code",
                schema: "juicestore",
                table: "juice_agg_roots",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_juice_agg_roots_flavor",
                schema: "juicestore",
                table: "juice_agg_roots",
                column: "flavor");

            migrationBuilder.CreateIndex(
                name: "ix_juice_agg_roots_is_featured_sort_order",
                schema: "juicestore",
                table: "juice_agg_roots",
                columns: new[] { "is_featured", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_master_data_audit_log_entities_aggregate_id_aggregate_ident",
                schema: "alcedoframework",
                table: "master_data_audit_log_entities",
                columns: new[] { "aggregate_id", "aggregate_identity" });

            migrationBuilder.CreateIndex(
                name: "ix_message_config_agg_roots_code",
                schema: "alcedonotification",
                table: "message_config_agg_roots",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_config_agg_roots_message_type_code",
                schema: "alcedonotification",
                table: "message_config_agg_roots",
                column: "message_type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_config_template_relations_entities_message_config_i",
                schema: "alcedonotification",
                table: "message_config_template_relations_entities",
                columns: new[] { "message_config_id", "message_template_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_config_template_relations_entities_message_template",
                schema: "alcedonotification",
                table: "message_config_template_relations_entities",
                column: "message_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_template_agg_roots_code",
                schema: "alcedonotification",
                table: "message_template_agg_roots",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_enqueue_time",
                schema: "masstransit",
                table: "outbox_message",
                column: "enqueue_time");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_expiration_time",
                schema: "masstransit",
                table: "outbox_message",
                column: "expiration_time");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_inbox_message_id_inbox_consumer_id_sequence_",
                schema: "masstransit",
                table: "outbox_message",
                columns: new[] { "inbox_message_id", "inbox_consumer_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_outbox_id_sequence_number",
                schema: "masstransit",
                table: "outbox_message",
                columns: new[] { "outbox_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_state_created",
                schema: "masstransit",
                table: "outbox_state",
                column: "created");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_req_recovery",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_inst_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "instance_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_nm_gp",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                columns: new[] { "sched_name", "trigger_name", "trigger_group" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_j_req_recovery",
                schema: "quartz",
                table: "qrtz_job_details",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_next_fire_time",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "next_fire_time");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "next_fire_time", "trigger_state" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_state",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "trigger_state");

            migrationBuilder.CreateIndex(
                name: "ix_qrtz_triggers_sched_name_job_name_job_group",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "sched_name", "job_name", "job_group" });

            migrationBuilder.CreateIndex(
                name: "ix_role_aggs_code",
                schema: "alcedoidentity",
                table: "role_aggs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_relations_role_id_permission_endpoint",
                schema: "alcedoidentity",
                table: "role_permissions_relations",
                columns: new[] { "role_id", "permission_endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_aggs_code",
                schema: "alcedoidentity",
                table: "user_aggs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_aggs_login_name",
                schema: "alcedoidentity",
                table: "user_aggs",
                column: "login_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAgg_Root_OnlyOne",
                schema: "alcedoidentity",
                table: "user_aggs",
                column: "is_root_user",
                unique: true,
                filter: "is_root_user = true");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_relations_role_id",
                schema: "alcedoidentity",
                table: "user_roles_relations",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_relations_user_id_role_id",
                schema: "alcedoidentity",
                table: "user_roles_relations",
                columns: new[] { "user_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_log_entities",
                schema: "alcedoadmin");

            migrationBuilder.DropTable(
                name: "formula_execution_log_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "formula_expression_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "formula_input_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "formula_output_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "history_formula_expression_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "history_formula_input_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "history_formula_output_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "history_user_entities",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "juice_agg_roots",
                schema: "juicestore");

            migrationBuilder.DropTable(
                name: "login_log_entities",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "master_data_audit_log_entities",
                schema: "alcedoframework");

            migrationBuilder.DropTable(
                name: "message_config_template_relations_entities",
                schema: "alcedonotification");

            migrationBuilder.DropTable(
                name: "message_error_entities",
                schema: "alcedonotification");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "masstransit");

            migrationBuilder.DropTable(
                name: "qrtz_blob_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_calendars",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_cron_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_fired_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_locks",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_paused_trigger_grps",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_scheduler_state",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_simple_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_simprop_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "role_permissions_relations",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "storage_gc");

            migrationBuilder.DropTable(
                name: "user_roles_relations",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "formula_agg_roots",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "history_formula_entities",
                schema: "alcedoformulaengine");

            migrationBuilder.DropTable(
                name: "message_config_agg_roots",
                schema: "alcedonotification");

            migrationBuilder.DropTable(
                name: "message_template_agg_roots",
                schema: "alcedonotification");

            migrationBuilder.DropTable(
                name: "inbox_state",
                schema: "masstransit");

            migrationBuilder.DropTable(
                name: "outbox_state",
                schema: "masstransit");

            migrationBuilder.DropTable(
                name: "qrtz_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "role_aggs",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "user_aggs",
                schema: "alcedoidentity");

            migrationBuilder.DropTable(
                name: "qrtz_job_details",
                schema: "quartz");
        }
    }
}
