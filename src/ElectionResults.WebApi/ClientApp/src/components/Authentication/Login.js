import React, { useState } from 'react';
import { useForm } from "react-hook-form";
import EmailInput from "../Form/EmailInput";
import PasswordInput from "../Form/PasswordInput";
import { useLogin } from "../../hooks/useLogin";
import { Button, Col, Form, Spinner } from "reactstrap";

import style from './Login.module.css';

const Login = () => {
  const { handleSubmit, register, errors } = useForm();

  const [state, setState] = useState(
    {
      login: false
    }
  );
  const { isLoading, response } = useLogin(state.login);
  const onSubmit = data => {
    setState(
      {
        login: true
      }
    );
  };

  if (response) {
    // TODO do some cookie/header persistence and then redirecting here
    console.log('res', response);
  }

  return (
    <Col md={ { size: 10, offset: 1 } } className={ style.formWrapper }>
      <Form onSubmit={ handleSubmit(onSubmit) }>
        <EmailInput
          required={ true }
          name={ 'email' }
          register={ register }
          errors={ errors }
          placeholder={ 'Please enter your email address' }
        />

        <PasswordInput
          required={ true }
          name={ 'password' }
          register={ register }
          errors={ errors }
          placeholder={ 'Please enter your password' }
        />
        <Button disabled={ isLoading }>Login</Button>
        {
          isLoading && <Spinner size="sm" color="secondary"/>
        }
      </Form>
    </Col>
  );
};

export default Login;
