CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'alcedoformulaengine') THEN
            CREATE SCHEMA alcedoformulaengine;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'alcedoidentity') THEN
            CREATE SCHEMA alcedoidentity;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'masstransit') THEN
            CREATE SCHEMA masstransit;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'juicestore') THEN
            CREATE SCHEMA juicestore;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'alcedoframework') THEN
            CREATE SCHEMA alcedoframework;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'alcedonotification') THEN
            CREATE SCHEMA alcedonotification;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'quartz') THEN
            CREATE SCHEMA quartz;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS pg_trgm;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.formula_agg_roots (
        id uuid NOT NULL,
        description text NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        CONSTRAINT pk_formula_agg_roots PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoformulaengine.formula_agg_roots IS 'formula_agg_roots';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.formula_execution_log_entities (
        id uuid NOT NULL,
        fomula_code character varying(512) NOT NULL,
        formula_version character varying(512) NOT NULL,
        input_json text NOT NULL,
        output_json text NOT NULL,
        executed_at timestamp with time zone NOT NULL,
        duration_ms bigint NOT NULL,
        CONSTRAINT pk_formula_execution_log_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoformulaengine.formula_execution_log_entities IS 'formula_execution_log_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.history_formula_entities (
        id uuid NOT NULL,
        code character varying(512) NOT NULL,
        description text NOT NULL,
        original_version integer NOT NULL,
        CONSTRAINT pk_history_formula_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoformulaengine.history_formula_entities IS 'history_formula_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.history_user_entities (
        id uuid NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(512) NOT NULL,
        login_name character varying(512) NOT NULL,
        user_name character varying(512),
        email character varying(512),
        contact character varying(512),
        is_locked boolean NOT NULL,
        join_time timestamp with time zone NOT NULL,
        archived_at timestamp with time zone NOT NULL,
        operator_id uuid NOT NULL,
        operator_code character varying(512) NOT NULL,
        operator_name character varying(512) NOT NULL,
        CONSTRAINT pk_history_user_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoidentity.history_user_entities IS 'history_user_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE masstransit.inbox_state (
        id bigint GENERATED BY DEFAULT AS IDENTITY,
        message_id uuid NOT NULL,
        consumer_id uuid NOT NULL,
        lock_id uuid NOT NULL,
        row_version bytea,
        received timestamp with time zone NOT NULL,
        receive_count integer NOT NULL,
        expiration_time timestamp with time zone,
        consumed timestamp with time zone,
        delivered timestamp with time zone,
        last_sequence_number bigint,
        CONSTRAINT pk_inbox_state PRIMARY KEY (id),
        CONSTRAINT ak_inbox_state_message_id_consumer_id UNIQUE (message_id, consumer_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE juicestore.juice_agg_roots (
        id uuid NOT NULL,
        description text NOT NULL,
        flavor character varying(100) NOT NULL,
        volume_ml integer NOT NULL,
        price numeric(18,2) NOT NULL,
        image_url character varying(2048),
        is_featured boolean NOT NULL,
        sort_order integer NOT NULL,
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(200) NOT NULL,
        status character varying(50) NOT NULL,
        creator_id uuid NOT NULL,
        creator_code character varying(512) NOT NULL,
        creator_name character varying(512) NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        CONSTRAINT pk_juice_agg_roots PRIMARY KEY (id)
    );
    COMMENT ON TABLE juicestore.juice_agg_roots IS 'juice_agg_roots';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.login_log_entities (
        id uuid NOT NULL,
        user_id uuid,
        login_name character varying(512) NOT NULL,
        ip_address character varying(512) NOT NULL,
        is_success boolean NOT NULL,
        session_id uuid,
        fail_reason character varying(512),
        login_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_login_log_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoidentity.login_log_entities IS 'login_log_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoframework.master_data_audit_log_entities (
        id uuid NOT NULL,
        aggregate_id uuid NOT NULL,
        aggregate_identity character varying(512) NOT NULL,
        operation_name character varying(512) NOT NULL,
        from_status_name character varying(512) NOT NULL,
        to_status_name character varying(512) NOT NULL,
        operated_at timestamp with time zone NOT NULL,
        operator_id uuid NOT NULL,
        operator_code character varying(512) NOT NULL,
        operator_name character varying(512) NOT NULL,
        note text,
        CONSTRAINT pk_master_data_audit_log_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoframework.master_data_audit_log_entities IS 'master_data_audit_log_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedonotification.message_config_agg_roots (
        id uuid NOT NULL,
        message_type_code character varying(512) NOT NULL,
        should_skip boolean NOT NULL,
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(512) NOT NULL,
        status character varying(50) NOT NULL,
        creator_id uuid NOT NULL,
        creator_code character varying(512) NOT NULL,
        creator_name character varying(512) NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        CONSTRAINT pk_message_config_agg_roots PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedonotification.message_config_agg_roots IS 'message_config_agg_roots';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedonotification.message_error_entities (
        id uuid NOT NULL,
        message_type_code character varying(512) NOT NULL,
        payload_json text NOT NULL,
        recipient_name character varying(512) NOT NULL,
        recipient_id uuid NOT NULL,
        recipient_type character varying(512),
        email character varying(512),
        phone character varying(512),
        error_msg text NOT NULL,
        failed_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_message_error_entities PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedonotification.message_error_entities IS 'message_error_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedonotification.message_template_agg_roots (
        id uuid NOT NULL,
        channel integer NOT NULL,
        title_template text,
        body_template text,
        external_template_id character varying(512),
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(512) NOT NULL,
        status character varying(50) NOT NULL,
        creator_id uuid NOT NULL,
        creator_code character varying(512) NOT NULL,
        creator_name character varying(512) NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        CONSTRAINT pk_message_template_agg_roots PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedonotification.message_template_agg_roots IS 'message_template_agg_roots';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE masstransit.outbox_state (
        outbox_id uuid NOT NULL,
        lock_id uuid NOT NULL,
        row_version bytea,
        created timestamp with time zone NOT NULL,
        delivered timestamp with time zone,
        last_sequence_number bigint,
        CONSTRAINT pk_outbox_state PRIMARY KEY (outbox_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_calendars (
        sched_name text NOT NULL,
        calendar_name text NOT NULL,
        calendar bytea NOT NULL,
        CONSTRAINT pk_qrtz_calendars PRIMARY KEY (sched_name, calendar_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_fired_triggers (
        sched_name text NOT NULL,
        entry_id text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        instance_name text NOT NULL,
        fired_time bigint NOT NULL,
        sched_time bigint NOT NULL,
        priority integer NOT NULL,
        state text NOT NULL,
        job_name text,
        job_group text,
        is_nonconcurrent bool NOT NULL,
        requests_recovery bool,
        CONSTRAINT pk_qrtz_fired_triggers PRIMARY KEY (sched_name, entry_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_job_details (
        sched_name text NOT NULL,
        job_name text NOT NULL,
        job_group text NOT NULL,
        description text,
        job_class_name text NOT NULL,
        is_durable bool NOT NULL,
        is_nonconcurrent bool NOT NULL,
        is_update_data bool NOT NULL,
        requests_recovery bool NOT NULL,
        job_data bytea,
        CONSTRAINT pk_qrtz_job_details PRIMARY KEY (sched_name, job_name, job_group)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_locks (
        sched_name text NOT NULL,
        lock_name text NOT NULL,
        CONSTRAINT pk_qrtz_locks PRIMARY KEY (sched_name, lock_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_paused_trigger_grps (
        sched_name text NOT NULL,
        trigger_group text NOT NULL,
        CONSTRAINT pk_qrtz_paused_trigger_grps PRIMARY KEY (sched_name, trigger_group)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_scheduler_state (
        sched_name text NOT NULL,
        instance_name text NOT NULL,
        last_checkin_time bigint NOT NULL,
        checkin_interval bigint NOT NULL,
        CONSTRAINT pk_qrtz_scheduler_state PRIMARY KEY (sched_name, instance_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.role_aggs (
        id uuid NOT NULL,
        description character varying(512),
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(512) NOT NULL,
        status character varying(50) NOT NULL,
        creator_id uuid NOT NULL,
        creator_code character varying(512) NOT NULL,
        creator_name character varying(512) NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        CONSTRAINT pk_role_aggs PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoidentity.role_aggs IS 'role_aggs';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE storage_gc (
        id bigint GENERATED BY DEFAULT AS IDENTITY,
        bucket character varying(512) NOT NULL,
        object_key character varying(512) NOT NULL,
        scheduled_at timestamp with time zone NOT NULL,
        retry_count integer NOT NULL,
        last_error TEXT,
        CONSTRAINT pk_storage_gc PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.user_aggs (
        id uuid NOT NULL,
        login_name character varying(512) NOT NULL,
        user_name character varying(512),
        email character varying(512),
        contact character varying(512),
        password_hash character varying(512) NOT NULL,
        requires_password_change boolean NOT NULL,
        two_factor_secret character varying(512),
        use_two_factor_authenticator boolean NOT NULL,
        current_session uuid,
        join_time timestamp with time zone,
        is_locked boolean NOT NULL,
        is_root_user boolean NOT NULL,
        version integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        code character varying(512) NOT NULL,
        name character varying(512) NOT NULL,
        status character varying(50) NOT NULL,
        creator_id uuid NOT NULL,
        creator_code character varying(512) NOT NULL,
        creator_name character varying(512) NOT NULL,
        updater_id uuid NOT NULL,
        updater_code character varying(512) NOT NULL,
        updater_name character varying(512) NOT NULL,
        CONSTRAINT pk_user_aggs PRIMARY KEY (id)
    );
    COMMENT ON TABLE alcedoidentity.user_aggs IS 'user_aggs';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.formula_expression_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        "order" integer NOT NULL,
        var_name character varying(512) NOT NULL,
        expression text NOT NULL,
        note text,
        CONSTRAINT pk_formula_expression_entities PRIMARY KEY (id),
        CONSTRAINT fk_formula_expression_entities_formula_agg_roots_formula_id FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.formula_agg_roots (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.formula_expression_entities IS 'formula_expression_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.formula_input_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        var_name character varying(512) NOT NULL,
        var_description text,
        var_type character varying(512) NOT NULL,
        valid_condition text,
        default_value character varying(512),
        CONSTRAINT pk_formula_input_entities PRIMARY KEY (id),
        CONSTRAINT fk_formula_input_entities_formula_agg_roots_formula_id FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.formula_agg_roots (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.formula_input_entities IS 'formula_input_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.formula_output_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        var_name character varying(512) NOT NULL,
        var_description text,
        var_type character varying(512) NOT NULL,
        expression text NOT NULL,
        CONSTRAINT pk_formula_output_entities PRIMARY KEY (id),
        CONSTRAINT fk_formula_output_entities_formula_agg_roots_formula_id FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.formula_agg_roots (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.formula_output_entities IS 'formula_output_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.history_formula_expression_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        "order" integer NOT NULL,
        var_name character varying(512) NOT NULL,
        expression text NOT NULL,
        note text,
        CONSTRAINT pk_history_formula_expression_entities PRIMARY KEY (id),
        CONSTRAINT fk_history_formula_expression_entities_history_formula_entitie FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.history_formula_entities (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.history_formula_expression_entities IS 'history_formula_expression_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.history_formula_input_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        var_name character varying(512) NOT NULL,
        var_description text,
        var_type character varying(512) NOT NULL,
        valid_condition text,
        default_value character varying(512),
        CONSTRAINT pk_history_formula_input_entities PRIMARY KEY (id),
        CONSTRAINT fk_history_formula_input_entities_history_formula_entities_for FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.history_formula_entities (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.history_formula_input_entities IS 'history_formula_input_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoformulaengine.history_formula_output_entities (
        id uuid NOT NULL,
        formula_id uuid NOT NULL,
        var_name character varying(512) NOT NULL,
        var_description text,
        var_type character varying(512) NOT NULL,
        expression text NOT NULL,
        CONSTRAINT pk_history_formula_output_entities PRIMARY KEY (id),
        CONSTRAINT fk_history_formula_output_entities_history_formula_entities_fo FOREIGN KEY (formula_id) REFERENCES alcedoformulaengine.history_formula_entities (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoformulaengine.history_formula_output_entities IS 'history_formula_output_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedonotification.message_config_template_relations_entities (
        id uuid NOT NULL,
        message_config_id uuid NOT NULL,
        message_template_id uuid NOT NULL,
        CONSTRAINT pk_message_config_template_relations_entities PRIMARY KEY (id),
        CONSTRAINT fk_message_config_template_relations_entities_message_config_a FOREIGN KEY (message_config_id) REFERENCES alcedonotification.message_config_agg_roots (id) ON DELETE CASCADE,
        CONSTRAINT fk_message_config_template_relations_entities_message_template FOREIGN KEY (message_template_id) REFERENCES alcedonotification.message_template_agg_roots (id) ON DELETE RESTRICT
    );
    COMMENT ON TABLE alcedonotification.message_config_template_relations_entities IS 'message_config_template_relations_entities';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE masstransit.outbox_message (
        sequence_number bigint GENERATED BY DEFAULT AS IDENTITY,
        enqueue_time timestamp with time zone,
        sent_time timestamp with time zone NOT NULL,
        headers text,
        properties text,
        inbox_message_id uuid,
        inbox_consumer_id uuid,
        outbox_id uuid,
        message_id uuid NOT NULL,
        content_type character varying(256) NOT NULL,
        message_type text NOT NULL,
        body text NOT NULL,
        conversation_id uuid,
        correlation_id uuid,
        initiator_id uuid,
        request_id uuid,
        source_address character varying(256),
        destination_address character varying(256),
        response_address character varying(256),
        fault_address character varying(256),
        expiration_time timestamp with time zone,
        CONSTRAINT pk_outbox_message PRIMARY KEY (sequence_number),
        CONSTRAINT fk_outbox_message_inbox_state_inbox_message_id_inbox_consumer_ FOREIGN KEY (inbox_message_id, inbox_consumer_id) REFERENCES masstransit.inbox_state (message_id, consumer_id),
        CONSTRAINT fk_outbox_message_outbox_state_outbox_id FOREIGN KEY (outbox_id) REFERENCES masstransit.outbox_state (outbox_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        job_name text NOT NULL,
        job_group text NOT NULL,
        description text,
        next_fire_time bigint,
        prev_fire_time bigint,
        priority integer,
        trigger_state text NOT NULL,
        trigger_type text NOT NULL,
        start_time bigint NOT NULL,
        end_time bigint,
        calendar_name text,
        misfire_instr smallint,
        job_data bytea,
        CONSTRAINT pk_qrtz_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_triggers_qrtz_job_details_sched_name_job_name_job_group FOREIGN KEY (sched_name, job_name, job_group) REFERENCES quartz.qrtz_job_details (sched_name, job_name, job_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.role_permissions_relations (
        id uuid NOT NULL,
        role_id uuid NOT NULL,
        permission_endpoint character varying(512) NOT NULL,
        CONSTRAINT pk_role_permissions_relations PRIMARY KEY (id),
        CONSTRAINT fk_role_permissions_relations_role_aggs_role_id FOREIGN KEY (role_id) REFERENCES alcedoidentity.role_aggs (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoidentity.role_permissions_relations IS 'role_permissions_relations';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE alcedoidentity.user_roles_relations (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT pk_user_roles_relations PRIMARY KEY (id),
        CONSTRAINT fk_user_roles_relations_role_aggs_role_id FOREIGN KEY (role_id) REFERENCES alcedoidentity.role_aggs (id) ON DELETE RESTRICT,
        CONSTRAINT fk_user_roles_relations_user_aggs_user_id FOREIGN KEY (user_id) REFERENCES alcedoidentity.user_aggs (id) ON DELETE CASCADE
    );
    COMMENT ON TABLE alcedoidentity.user_roles_relations IS 'user_roles_relations';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_blob_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        blob_data bytea,
        CONSTRAINT pk_qrtz_blob_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_blob_triggers_qrtz_triggers_sched_name_trigger_name_tr FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_cron_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        cron_expression text NOT NULL,
        time_zone_id text,
        CONSTRAINT pk_qrtz_cron_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_cron_triggers_qrtz_triggers_sched_name_trigger_name_tr FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_simple_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        repeat_count bigint NOT NULL,
        repeat_interval bigint NOT NULL,
        times_triggered bigint NOT NULL,
        CONSTRAINT pk_qrtz_simple_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_simple_triggers_qrtz_triggers_sched_name_trigger_name_ FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE TABLE quartz.qrtz_simprop_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        str_prop_1 text,
        str_prop_2 text,
        str_prop_3 text,
        int_prop_1 integer,
        int_prop_2 integer,
        long_prop_1 bigint,
        long_prop_2 bigint,
        dec_prop_1 numeric(18,6),
        dec_prop_2 numeric(18,6),
        bool_prop_1 bool,
        bool_prop_2 bool,
        time_zone_id text,
        CONSTRAINT pk_qrtz_simprop_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_simprop_triggers_qrtz_triggers_sched_name_trigger_name FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_formula_agg_roots_code ON alcedoformulaengine.formula_agg_roots (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_formula_agg_roots_code_version ON alcedoformulaengine.formula_agg_roots (code, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_formula_expression_entities_formula_id_order ON alcedoformulaengine.formula_expression_entities (formula_id, "order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_formula_expression_entities_formula_id_var_name ON alcedoformulaengine.formula_expression_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_formula_input_entities_formula_id_var_name ON alcedoformulaengine.formula_input_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_formula_output_entities_formula_id_var_name ON alcedoformulaengine.formula_output_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_entities_code ON alcedoformulaengine.history_formula_entities (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_entities_code_original_version ON alcedoformulaengine.history_formula_entities (code, original_version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_expression_entities_formula_id_order ON alcedoformulaengine.history_formula_expression_entities (formula_id, "order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_expression_entities_formula_id_var_name ON alcedoformulaengine.history_formula_expression_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_input_entities_formula_id_var_name ON alcedoformulaengine.history_formula_input_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_history_formula_output_entities_formula_id_var_name ON alcedoformulaengine.history_formula_output_entities (formula_id, var_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_inbox_state_delivered ON masstransit.inbox_state (delivered);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_juice_agg_roots_code ON juicestore.juice_agg_roots (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_juice_agg_roots_flavor ON juicestore.juice_agg_roots (flavor);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_juice_agg_roots_is_featured_sort_order ON juicestore.juice_agg_roots (is_featured, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_master_data_audit_log_entities_aggregate_id_aggregate_ident ON alcedoframework.master_data_audit_log_entities (aggregate_id, aggregate_identity);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_message_config_agg_roots_code ON alcedonotification.message_config_agg_roots (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_message_config_agg_roots_message_type_code ON alcedonotification.message_config_agg_roots (message_type_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_message_config_template_relations_entities_message_config_i ON alcedonotification.message_config_template_relations_entities (message_config_id, message_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_message_config_template_relations_entities_message_template ON alcedonotification.message_config_template_relations_entities (message_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_message_template_agg_roots_code ON alcedonotification.message_template_agg_roots (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_outbox_message_enqueue_time ON masstransit.outbox_message (enqueue_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_outbox_message_expiration_time ON masstransit.outbox_message (expiration_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_outbox_message_inbox_message_id_inbox_consumer_id_sequence_ ON masstransit.outbox_message (inbox_message_id, inbox_consumer_id, sequence_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_outbox_message_outbox_id_sequence_number ON masstransit.outbox_message (outbox_id, sequence_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_outbox_state_created ON masstransit.outbox_state (created);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_job_group ON quartz.qrtz_fired_triggers (job_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_job_name ON quartz.qrtz_fired_triggers (job_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_job_req_recovery ON quartz.qrtz_fired_triggers (requests_recovery);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_trig_group ON quartz.qrtz_fired_triggers (trigger_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_trig_inst_name ON quartz.qrtz_fired_triggers (instance_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_trig_name ON quartz.qrtz_fired_triggers (trigger_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_ft_trig_nm_gp ON quartz.qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_j_req_recovery ON quartz.qrtz_job_details (requests_recovery);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_t_next_fire_time ON quartz.qrtz_triggers (next_fire_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_t_nft_st ON quartz.qrtz_triggers (next_fire_time, trigger_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX idx_qrtz_t_state ON quartz.qrtz_triggers (trigger_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_qrtz_triggers_sched_name_job_name_job_group ON quartz.qrtz_triggers (sched_name, job_name, job_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_role_aggs_code ON alcedoidentity.role_aggs (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_role_permissions_relations_role_id_permission_endpoint ON alcedoidentity.role_permissions_relations (role_id, permission_endpoint);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_user_aggs_code ON alcedoidentity.user_aggs (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_user_aggs_login_name ON alcedoidentity.user_aggs (login_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_UserAgg_Root_OnlyOne" ON alcedoidentity.user_aggs (is_root_user) WHERE is_root_user = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE INDEX ix_user_roles_relations_role_id ON alcedoidentity.user_roles_relations (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_user_roles_relations_user_id_role_id ON alcedoidentity.user_roles_relations (user_id, role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112112735_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261112112735_InitialCreate', '10.0.8');
    END IF;
END $EF$;
COMMIT;
