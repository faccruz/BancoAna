CREATE TABLE IF NOT EXISTS contacorrente (
  idcontacorrente TEXT(37) PRIMARY KEY,
  numero INTEGER(10) NOT NULL,
  nome TEXT(100) NOT NULL,
  ativo INTEGER(1) NOT NULL DEFAULT 0,
  senha TEXT(100) NOT NULL,
  salt TEXT(100) NOT NULL,
  Cpf TEXT(100) UNIQUE,
  CHECK (ativo in (0,1))
);

CREATE TABLE IF NOT EXISTS movimento (
  idmovimento TEXT(37) PRIMARY KEY,
  idcontacorrente TEXT(37) NOT NULL,
  datamovimento TEXT(25) NOT NULL,
  tipomovimento TEXT(1) NOT NULL,
  valor REAL NOT NULL,
  CHECK (tipomovimento in ('C','D')),
  FOREIGN KEY(idcontacorrente) REFERENCES contacorrente(idcontacorrente)
);

CREATE TABLE IF NOT EXISTS idempotencia (
  chave_idempotencia TEXT(37) PRIMARY KEY,
  requisicao TEXT(1000),
  resultado TEXT(1000)
);

CREATE TABLE IF NOT EXISTS tarifa (
  idtarifa TEXT(37) PRIMARY KEY,
  idcontacorrente TEXT(37) NOT NULL,
  datamovimento TEXT(25) NOT NULL,
  valor REAL NOT NULL,
  FOREIGN KEY(idcontacorrente) REFERENCES contacorrente(idcontacorrente)
);

CREATE TABLE IF NOT EXISTS transferencia (
  idtransferencia TEXT(37) PRIMARY KEY,
  idcontacorrente_origem TEXT(37) NOT NULL,
  idcontacorrente_destino TEXT(37) NOT NULL,
  datamovimento TEXT(25) NOT NULL,
  valor REAL NOT NULL,
  FOREIGN KEY(idcontacorrente_origem) REFERENCES contacorrente(idcontacorrente),
  FOREIGN KEY(idcontacorrente_destino) REFERENCES contacorrente(idcontacorrente)
);
