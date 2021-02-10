create table progress
(
    key varchar not null
        constraint progress_pk
            primary key,
    value varchar not null
);